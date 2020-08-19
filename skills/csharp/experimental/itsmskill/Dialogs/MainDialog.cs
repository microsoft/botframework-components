// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using ITSMSkill.Models;
using ITSMSkill.Models.Actions;
using ITSMSkill.Responses.Main;
using ITSMSkill.Responses.Shared;
using ITSMSkill.Services;
using ITSMSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using SkillServiceLibrary.Utilities;

namespace ITSMSkill.Dialogs
{
    // Dialog providing activity routing and message/event processing.
    public class MainDialog : ComponentDialog
    {
        private readonly BotServices _services;
        private readonly LocaleTemplateManager _templateManager;
        private readonly BotSettings _botSettings;
        private readonly IStatePropertyAccessor<SkillState> _stateAccessor;

        public MainDialog(
            IServiceProvider serviceProvider)
            : base(nameof(MainDialog))
        {
            _services = serviceProvider.GetService<BotServices>();
            _templateManager = serviceProvider.GetService<LocaleTemplateManager>();
            _botSettings = serviceProvider.GetService<BotSettings>();

            // Create conversation state properties
            var conversationState = serviceProvider.GetService<ConversationState>();
            _stateAccessor = conversationState.CreateProperty<SkillState>(nameof(SkillState));

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
            AddDialog(serviceProvider.GetService<CreateTicketDialog>());
            AddDialog(serviceProvider.GetService<UpdateTicketDialog>());
            AddDialog(serviceProvider.GetService<ShowTicketDialog>());
            AddDialog(serviceProvider.GetService<CloseTicketDialog>());
            AddDialog(serviceProvider.GetService<ShowKnowledgeDialog>());
            AddDialog(serviceProvider.GetService<CreateSubscriptionDialog>());
        }

        // Runs when the dialog is started.
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(innerDc.Context.Activity.Text))
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                var skillResult = await localizedServices.LuisServices["ITSM"].RecognizeAsync<ITSMLuis>(innerDc.Context, cancellationToken);
                innerDc.Context.TurnState.Add(StateProperties.ITSMLuisResult, skillResult);

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
                var skillResult = await localizedServices.LuisServices["ITSM"].RecognizeAsync<ITSMLuis>(innerDc.Context, cancellationToken);
                innerDc.Context.TurnState.Add(StateProperties.ITSMLuisResult, skillResult);

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

                var state = await _stateAccessor.GetAsync(innerDc.Context, () => new SkillState(), cancellationToken);

                if (generalScore > 0.5)
                {
                    state.GeneralIntent = generalIntent;
                    switch (generalIntent)
                    {
                        case GeneralLuis.Intent.Cancel:
                            {
                                await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivity(MainResponses.CancelMessage), cancellationToken);
                                await innerDc.CancelAllDialogsAsync(cancellationToken);
                                if (innerDc.Context.IsSkill())
                                {
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
                else
                {
                    state.GeneralIntent = GeneralLuis.Intent.None;
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
            // Clear IsAction before dialog
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new SkillState(), cancellationToken);
            state.IsAction = false;

            var activity = stepContext.Context.Activity;

            if (activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(activity.Text))
            {
                // Get current cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Get skill LUIS model from configuration.
                localizedServices.LuisServices.TryGetValue("ITSM", out var luisService);

                if (luisService != null)
                {
                    var result = stepContext.Context.TurnState.Get<ITSMLuis>(StateProperties.ITSMLuisResult);
                    var (intent, score) = result.TopIntent();

                    // TODO filter bad ones
                    if (score < 0.4)
                    {
                        intent = ITSMLuis.Intent.None;
                    }

                    if (intent != ITSMLuis.Intent.None)
                    {
                        state.DigestLuisResult(result, (ITSMLuis.Intent)intent);
                    }

                    switch (intent)
                    {
                        case ITSMLuis.Intent.TicketCreate:
                            {
                                return await stepContext.BeginDialogAsync(nameof(CreateTicketDialog), cancellationToken: cancellationToken);
                            }

                        case ITSMLuis.Intent.TicketUpdate:
                            {
                                return await stepContext.BeginDialogAsync(nameof(UpdateTicketDialog), cancellationToken: cancellationToken);
                            }

                        case ITSMLuis.Intent.TicketShow:
                            {
                                return await stepContext.BeginDialogAsync(nameof(ShowTicketDialog), cancellationToken: cancellationToken);
                            }

                        case ITSMLuis.Intent.TicketClose:
                            {
                                return await stepContext.BeginDialogAsync(nameof(CloseTicketDialog), cancellationToken: cancellationToken);
                            }

                        case ITSMLuis.Intent.KnowledgeShow:
                            {
                                return await stepContext.BeginDialogAsync(nameof(ShowKnowledgeDialog), cancellationToken: cancellationToken);
                            }

                        case ITSMLuis.Intent.CreateSubscription:
                            {
                                return await stepContext.BeginDialogAsync(nameof(CreateSubscriptionDialog), cancellationToken: cancellationToken);
                            }

                        case ITSMLuis.Intent.None:
                        default:
                            {
                                if (_botSettings.FallbackToKnowledge)
                                {
                                    // TODO always use show knowledge here
                                    state.ClearLuisResult();
                                    state.TicketTitle = activity.Text;

                                    var option = new BaseOption
                                    {
                                        ConfirmSearch = false,
                                    };

                                    return await stepContext.BeginDialogAsync(nameof(ShowKnowledgeDialog), option, cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    // intent was identified but not yet implemented
                                    await stepContext.Context.SendActivityAsync(_templateManager.GenerateActivity(MainResponses.FeatureNotAvailable), cancellationToken);
                                    return await stepContext.NextAsync(cancellationToken: cancellationToken);
                                }
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
                        case ActionNames.CreateTicket:
                            {
                                return await ProcessActionAsync<CreateTicketInput>(ITSMLuis.Intent.TicketCreate, nameof(CreateTicketDialog), stepContext, cancellationToken);
                            }

                        case ActionNames.UpdateTicket:
                            {
                                return await ProcessActionAsync<UpdateTicketInput>(ITSMLuis.Intent.TicketUpdate, nameof(UpdateTicketDialog), stepContext, cancellationToken);
                            }

                        case ActionNames.ShowTicket:
                            {
                                return await ProcessActionAsync<ShowTicketInput>(ITSMLuis.Intent.TicketShow, nameof(ShowTicketDialog), stepContext, cancellationToken);
                            }

                        case ActionNames.CloseTicket:
                            {
                                return await ProcessActionAsync<CloseTicketInput>(ITSMLuis.Intent.TicketClose, nameof(CloseTicketDialog), stepContext, cancellationToken);
                            }

                        case ActionNames.ShowKnowledge:
                            {
                                return await ProcessActionAsync<ShowKnowledgeInput>(ITSMLuis.Intent.KnowledgeShow, nameof(ShowKnowledgeDialog), stepContext, cancellationToken);
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
            if (stepContext.Context.IsSkill())
            {
                var result = stepContext.Result;

                var state = await _stateAccessor.GetAsync(stepContext.Context, () => new SkillState(), cancellationToken);
                if (state.IsAction && result as ActionResult == null)
                {
                    result = new ActionResult(false);
                }

                return await stepContext.EndDialogAsync(result, cancellationToken);
            }
            else
            {
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

        private async Task<DialogTurnResult> ProcessActionAsync<T>(ITSMLuis.Intent intent, string dialogId, WaterfallStepContext stepContext, CancellationToken cancellationToken)
             where T : IActionInput
        {
            ITSMLuis result = null;
            T actionData = null;

            var ev = stepContext.Context.Activity.AsEventActivity();
            if (ev.Value is JObject eventValue)
            {
                actionData = eventValue.ToObject<T>();
                result = actionData.CreateLuis();
            }

            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new SkillState(), cancellationToken);
            state.DigestLuisResult(result, intent);
            state.IsAction = true;

            if (actionData != null)
            {
                actionData.ProcessAfterDigest(state);
            }

            // Don't confirm if provided in action
            var option = new BaseOption
            {
                ConfirmSearch = false,
            };

            return await stepContext.BeginDialogAsync(dialogId, option, cancellationToken);
        }
    }
}