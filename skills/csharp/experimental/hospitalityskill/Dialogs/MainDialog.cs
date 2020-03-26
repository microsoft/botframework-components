// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using HospitalitySkill.Models;
using HospitalitySkill.Models.ActionDefinitions;
using HospitalitySkill.Responses.Main;
using HospitalitySkill.Responses.Shared;
using HospitalitySkill.Services;
using HospitalitySkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using SkillServiceLibrary.Utilities;

namespace HospitalitySkill.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly BotServices _services;
        private readonly IHotelService _hotelService;
        private readonly LocaleTemplateManager _templateManager;
        private readonly IStatePropertyAccessor<HospitalitySkillState> _stateAccessor;
        private readonly IStatePropertyAccessor<HospitalityUserSkillState> _userStateAccessor;

        public MainDialog(
            IServiceProvider serviceProvider)
            : base(nameof(MainDialog))
        {
            _services = serviceProvider.GetService<BotServices>();
            _hotelService = serviceProvider.GetService<IHotelService>();
            _templateManager = serviceProvider.GetService<LocaleTemplateManager>();

            // Create conversation state properties
            var conversationState = serviceProvider.GetService<ConversationState>();
            _stateAccessor = conversationState.CreateProperty<HospitalitySkillState>(nameof(HospitalitySkillState));

            var userState = serviceProvider.GetService<UserState>();
            _userStateAccessor = userState.CreateProperty<HospitalityUserSkillState>(nameof(HospitalityUserSkillState));

            var steps = new WaterfallStep[]
            {
                IntroStepAsync,
                RouteStepAsync,
                FinalStepAsync,
            };

            AddDialog(new WaterfallDialog(nameof(MainDialog), steps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            InitialDialogId = nameof(MainDialog);

            // Register dialogs
            AddDialog(serviceProvider.GetService<CheckOutDialog>());
            AddDialog(serviceProvider.GetService<LateCheckOutDialog>());
            AddDialog(serviceProvider.GetService<ExtendStayDialog>());
            AddDialog(serviceProvider.GetService<GetReservationDialog>());
            AddDialog(serviceProvider.GetService<RequestItemDialog>());
            AddDialog(serviceProvider.GetService<RoomServiceDialog>());
        }

        // Runs when the dialog is started.
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(innerDc.Context.Activity.Text))
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                var skillResult = await localizedServices.LuisServices["Hospitality"].RecognizeAsync<HospitalityLuis>(innerDc.Context, cancellationToken);
                innerDc.Context.TurnState.Add(StateProperties.SkillLuisResult, skillResult);

                // Run LUIS recognition on General model and store result in turn state.
                var generalResult = await localizedServices.LuisServices["General"].RecognizeAsync<GeneralLuis>(innerDc.Context, cancellationToken);
                innerDc.Context.TurnState.Add(StateProperties.GeneralLuisResult, generalResult);

                // Check for any interruptions
                var interrupted = await InterruptDialogAsync(innerDc, cancellationToken);

                if (interrupted != null)
                {
                    // If dialog was interrupted, return interrupted result
                    return interrupted;
                }
            }

            return await base.OnBeginDialogAsync(innerDc, options, cancellationToken);
        }

        // Runs on every turn of the conversation.
        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(innerDc.Context.Activity.Text))
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                var skillResult = await localizedServices.LuisServices["Hospitality"].RecognizeAsync<HospitalityLuis>(innerDc.Context, cancellationToken);
                innerDc.Context.TurnState.Add(StateProperties.SkillLuisResult, skillResult);

                // Run LUIS recognition on General model and store result in turn state.
                var generalResult = await localizedServices.LuisServices["General"].RecognizeAsync<GeneralLuis>(innerDc.Context, cancellationToken);
                innerDc.Context.TurnState.Add(StateProperties.GeneralLuisResult, generalResult);

                // Check for any interruptions
                var interrupted = await InterruptDialogAsync(innerDc, cancellationToken);

                if (interrupted != null)
                {
                    // If dialog was interrupted, return interrupted result
                    return interrupted;
                }
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        // Runs on every turn of the conversation to check if the conversation should be interrupted.
        protected async Task<DialogTurnResult> InterruptDialogAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            DialogTurnResult interrupted = null;
            var activity = innerDc.Context.Activity;

            if (activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(activity.Text))
            {
                // Get connected LUIS result from turn state.
                var generalResult = innerDc.Context.TurnState.Get<GeneralLuis>(StateProperties.GeneralLuisResult);
                (var generalIntent, var generalScore) = generalResult.TopIntent();

                if (generalScore > 0.5)
                {
                    switch (generalIntent)
                    {
                        case GeneralLuis.Intent.Cancel:
                            {
                                await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivity(MainResponses.CancelMessage), cancellationToken);
                                await innerDc.CancelAllDialogsAsync(cancellationToken);
                                if (innerDc.Context.IsSkill())
                                {
                                    var state = await _stateAccessor.GetAsync(innerDc.Context, () => new HospitalitySkillState(), cancellationToken);
                                    interrupted = await innerDc.EndDialogAsync(state.IsAction ? new ActionResult(false) : null, cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    interrupted = await innerDc.BeginDialogAsync(InitialDialogId, cancellationToken: cancellationToken);
                                }

                                break;
                            }

                        case GeneralLuis.Intent.Help:
                            {
                                await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivity(MainResponses.HelpMessage), cancellationToken);
                                await innerDc.RepromptDialogAsync(cancellationToken);
                                interrupted = EndOfTurn;
                                break;
                            }

                        case GeneralLuis.Intent.Logout:
                            {
                                // Log user out of all accounts.
                                await LogUserOutAsync(innerDc, cancellationToken);

                                await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivity(MainResponses.LogOut), cancellationToken);
                                await innerDc.CancelAllDialogsAsync(cancellationToken);
                                if (innerDc.Context.IsSkill())
                                {
                                    var state = await _stateAccessor.GetAsync(innerDc.Context, () => new HospitalitySkillState(), cancellationToken);
                                    interrupted = await innerDc.EndDialogAsync(state.IsAction ? new ActionResult(false) : null, cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    interrupted = await innerDc.BeginDialogAsync(InitialDialogId, cancellationToken: cancellationToken);
                                }

                                break;
                            }
                    }
                }
            }

            return interrupted;
        }

        // Handles introduction/continuation prompt logic.
        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Context.IsSkill())
            {
                // If the bot is in skill mode, skip directly to route and do not prompt
                return await stepContext.NextAsync(cancellationToken: cancellationToken);
            }

            // If bot is in local mode, prompt with intro or continuation message
            var promptOptions = new PromptOptions
            {
                Prompt = stepContext.Options as Activity ?? _templateManager.GenerateActivity(MainResponses.FirstPromptMessage)
            };

            return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
        }

        // Handles routing to additional dialogs logic.
        private async Task<DialogTurnResult> RouteStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Reset before all dialogs
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new HospitalitySkillState(), cancellationToken);
            state.IsAction = false;

            var activity = stepContext.Context.Activity;

            if (activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(activity.Text))
            {
                // Get current cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Get skill LUIS model from configuration.
                localizedServices.LuisServices.TryGetValue("Hospitality", out var luisService);

                if (luisService != null)
                {
                    var result = stepContext.Context.TurnState.Get<HospitalityLuis>(StateProperties.SkillLuisResult);
                    var intent = result?.TopIntent().intent;

                    switch (intent)
                    {
                        case HospitalityLuis.Intent.CheckOut:
                            {
                                // handle checking out
                                return await stepContext.BeginDialogAsync(nameof(CheckOutDialog), cancellationToken: cancellationToken);
                            }

                        case HospitalityLuis.Intent.ExtendStay:
                            {
                                // extend reservation dates
                                return await stepContext.BeginDialogAsync(nameof(ExtendStayDialog), cancellationToken: cancellationToken);
                            }

                        case HospitalityLuis.Intent.LateCheckOut:
                            {
                                // set a late check out time
                                return await stepContext.BeginDialogAsync(nameof(LateCheckOutDialog), cancellationToken: cancellationToken);
                            }

                        case HospitalityLuis.Intent.GetReservationDetails:
                            {
                                // show reservation details card
                                return await stepContext.BeginDialogAsync(nameof(GetReservationDialog), cancellationToken: cancellationToken);
                            }

                        case HospitalityLuis.Intent.RequestItem:
                            {
                                // requesting item for room
                                return await stepContext.BeginDialogAsync(nameof(RequestItemDialog), cancellationToken: cancellationToken);
                            }

                        case HospitalityLuis.Intent.RoomService:
                            {
                                // ordering room service
                                return await stepContext.BeginDialogAsync(nameof(RoomServiceDialog), cancellationToken: cancellationToken);
                            }

                        case HospitalityLuis.Intent.None:
                            {
                                // No intent was identified, send confused message
                                await stepContext.Context.SendActivityAsync(_templateManager.GenerateActivity(SharedResponses.DidntUnderstandMessage), cancellationToken);
                                return await stepContext.NextAsync(cancellationToken: cancellationToken);
                            }

                        default:
                            {
                                // intent was identified but not yet implemented
                                await stepContext.Context.SendActivityAsync(_templateManager.GenerateActivity(MainResponses.FeatureNotAvailable), cancellationToken);
                                return await stepContext.NextAsync(cancellationToken: cancellationToken);
                            }
                    }
                }
                else
                {
                    throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
                }
            }
            else if (activity.Type == ActivityTypes.Event)
            {
                var ev = activity.AsEventActivity();

                if (!string.IsNullOrEmpty(ev.Name))
                {
                    switch (ev.Name)
                    {
                        case ActionNames.CheckOut:
                            {
                                return await ProcessAction<CheckOutInput>(nameof(CheckOutDialog), stepContext, cancellationToken);
                            }

                        case ActionNames.ExtendStay:
                            {
                                return await ProcessAction<ExtendStayInput>(nameof(ExtendStayDialog), stepContext, cancellationToken);
                            }

                        case ActionNames.LateCheckOut:
                            {
                                return await ProcessAction<LateCheckOutInput>(nameof(LateCheckOutDialog), stepContext, cancellationToken);
                            }

                        case ActionNames.GetReservationDetails:
                            {
                                return await ProcessAction<GetReservationDetailsInput>(nameof(GetReservationDialog), stepContext, cancellationToken);
                            }

                        case ActionNames.RequestItem:
                            {
                                return await ProcessAction<RequestItemInput>(nameof(RequestItemDialog), stepContext, cancellationToken);
                            }

                        case ActionNames.RoomService:
                            {
                                return await ProcessAction<RoomServiceInput>(nameof(RoomServiceDialog), stepContext, cancellationToken);
                            }

                        default:
                            {
                                await stepContext.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown Event '{ev.Name ?? "undefined"}' was received but not processed."), cancellationToken);
                                break;
                            }
                    }
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: "An event with no name was received but not processed."), cancellationToken);
                }
            }

            // If activity was unhandled, flow should continue to next step
            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }

        // Handles conversation cleanup.
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new HospitalitySkillState(), cancellationToken);

            if (stepContext.Context.IsSkill())
            {
                var result = stepContext.Result;

                if (state.IsAction && result as ActionResult == null)
                {
                    result = new ActionResult(false);
                }

                state.Clear();
                return await stepContext.EndDialogAsync(result, cancellationToken);
            }
            else
            {
                state.Clear();
                return await stepContext.ReplaceDialogAsync(InitialDialogId, _templateManager.GenerateActivity(SharedResponses.ActionEnded), cancellationToken);
            }
        }

        private async Task LogUserOutAsync(DialogContext dc, CancellationToken cancellationToken)
        {
            var supported = dc.Context.Adapter is IUserTokenProvider;
            if (supported)
            {
                var tokenProvider = (IUserTokenProvider)dc.Context.Adapter;

                // Sign out user
                var tokens = await tokenProvider.GetTokenStatusAsync(dc.Context, dc.Context.Activity.From.Id, cancellationToken: cancellationToken);
                foreach (var token in tokens)
                {
                    await tokenProvider.SignOutUserAsync(dc.Context, token.ConnectionName, cancellationToken: cancellationToken);
                }

                // Cancel all active dialogs
                await dc.CancelAllDialogsAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("OAuthPrompt.SignOutUser(): not supported by the current adapter");
            }
        }

        private async Task<DialogTurnResult> ProcessAction<T>(string dialogId, WaterfallStepContext stepContext, CancellationToken cancellationToken)
            where T : IActionInput
        {
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new HospitalitySkillState(), cancellationToken);
            state.IsAction = true;

            var ev = stepContext.Context.Activity.AsEventActivity();
            if (ev.Value is JObject eventValue)
            {
                var input = eventValue.ToObject<T>();
                await input.Process(stepContext.Context, _stateAccessor, _userStateAccessor, _hotelService, cancellationToken);
            }

            return await stepContext.BeginDialogAsync(dialogId, null, cancellationToken);
        }
    }
}
