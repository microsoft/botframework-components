﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using ITSMSkill.Models;
using ITSMSkill.Models.Actions;
using ITSMSkill.Responses.Main;
using ITSMSkill.Responses.Shared;
using ITSMSkill.Services;
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
        private BotServices _services;
        private ResponseManager _responseManager;
        private IStatePropertyAccessor<SkillState> _stateAccessor;

        public MainDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog))
        {
            _services = serviceProvider.GetService<BotServices>();
            _responseManager = serviceProvider.GetService<ResponseManager>();
            TelemetryClient = telemetryClient;

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

                if (interrupted)
                {
                    // If dialog was interrupted, return EndOfTurn
                    return EndOfTurn;
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

                if (interrupted)
                {
                    // If dialog was interrupted, return EndOfTurn
                    return EndOfTurn;
                }
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        // Runs on every turn of the conversation to check if the conversation should be interrupted.
        protected async Task<bool> InterruptDialogAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            var interrupted = false;
            var activity = innerDc.Context.Activity;

            if (activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(activity.Text))
            {
                // Get connected LUIS result from turn state.
                var generalResult = innerDc.Context.TurnState.Get<GeneralLuis>(StateProperties.GeneralLuisResult);
                (var generalIntent, var generalScore) = generalResult.TopIntent();

                var state = await _stateAccessor.GetAsync(innerDc.Context, () => new SkillState());

                if (generalScore > 0.5)
                {
                    state.GeneralIntent = generalIntent;
                    switch (generalIntent)
                    {
                        case GeneralLuis.Intent.Cancel:
                            {
                                await innerDc.Context.SendActivityAsync(_responseManager.GetResponse(MainResponses.CancelMessage));
                                await innerDc.CancelAllDialogsAsync();
                                await innerDc.BeginDialogAsync(InitialDialogId);
                                interrupted = true;
                                break;
                            }

                        case GeneralLuis.Intent.Help:
                            {
                                await innerDc.Context.SendActivityAsync(_responseManager.GetResponse(MainResponses.HelpMessage));
                                await innerDc.RepromptDialogAsync();
                                interrupted = true;
                                break;
                            }

                        case GeneralLuis.Intent.Logout:
                            {
                                // Log user out of all accounts.
                                await LogUserOut(innerDc);

                                await innerDc.Context.SendActivityAsync(_responseManager.GetResponse(MainResponses.LogOut));
                                await innerDc.CancelAllDialogsAsync();
                                await innerDc.BeginDialogAsync(InitialDialogId);
                                interrupted = true;
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
                return await stepContext.NextAsync();
            }
            else
            {
                // If bot is in local mode, prompt with intro or continuation message
                var promptOptions = new PromptOptions
                {
                    Prompt = stepContext.Options as Activity ?? _responseManager.GetResponse(MainResponses.WelcomeMessage)
                };

                return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
            }
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
                    var intent = result?.TopIntent().intent;

                    if (intent != null && intent != ITSMLuis.Intent.None)
                    {
                        state.DigestLuisResult(result, (ITSMLuis.Intent)intent);
                    }

                    switch (intent)
                    {
                        case ITSMLuis.Intent.TicketCreate:
                            {
                                return await stepContext.BeginDialogAsync(nameof(CreateTicketDialog));
                            }

                        case ITSMLuis.Intent.TicketUpdate:
                            {
                                return await stepContext.BeginDialogAsync(nameof(UpdateTicketDialog));
                            }

                        case ITSMLuis.Intent.TicketShow:
                            {
                                return await stepContext.BeginDialogAsync(nameof(ShowTicketDialog));
                            }

                        case ITSMLuis.Intent.TicketClose:
                            {
                                return await stepContext.BeginDialogAsync(nameof(CloseTicketDialog));
                            }

                        case ITSMLuis.Intent.KnowledgeShow:
                            {
                                return await stepContext.BeginDialogAsync(nameof(ShowKnowledgeDialog));
                            }

                        case ITSMLuis.Intent.None:
                        default:
                            {
                                // intent was identified but not yet implemented
                                await stepContext.Context.SendActivityAsync(_responseManager.GetResponse(MainResponses.FeatureNotAvailable));
                                return await stepContext.NextAsync();
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
                                return await ProcessAction<CreateTicketInput>(ITSMLuis.Intent.TicketCreate, nameof(CreateTicketDialog), stepContext, cancellationToken);
                            }

                        case ActionNames.UpdateTicket:
                            {
                                return await ProcessAction<UpdateTicketInput>(ITSMLuis.Intent.TicketUpdate, nameof(UpdateTicketDialog), stepContext, cancellationToken);
                            }

                        case ActionNames.ShowTicket:
                            {
                                return await ProcessAction<ShowTicketInput>(ITSMLuis.Intent.TicketShow, nameof(ShowTicketDialog), stepContext, cancellationToken);
                            }

                        case ActionNames.CloseTicket:
                            {
                                return await ProcessAction<CloseTicketInput>(ITSMLuis.Intent.TicketClose, nameof(CloseTicketDialog), stepContext, cancellationToken);
                            }

                        case ActionNames.ShowKnowledge:
                            {
                                return await ProcessAction<ShowKnowledgeInput>(ITSMLuis.Intent.KnowledgeShow, nameof(ShowKnowledgeDialog), stepContext, cancellationToken);
                            }

                        default:
                            {
                                await stepContext.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown Event '{ev.Name ?? "undefined"}' was received but not processed."));
                                break;
                            }
                    }
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"An event with no name was received but not processed."));
                }
            }

            // If activity was unhandled, flow should continue to next step
            return await stepContext.NextAsync();
        }

        // Handles conversation cleanup.
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Context.IsSkill())
            {
                // EndOfConversation activity should be passed back to indicate that VA should resume control of the conversation
                var endOfConversation = new Activity(ActivityTypes.EndOfConversation)
                {
                    Code = EndOfConversationCodes.CompletedSuccessfully,
                    Value = stepContext.Result,
                };

                var state = await _stateAccessor.GetAsync(stepContext.Context, () => new SkillState(), cancellationToken);
                if (state.IsAction)
                {
                    if (stepContext.Result == null)
                    {
                        endOfConversation.Value = new ActionResult(false);
                    }
                }

                await stepContext.Context.SendActivityAsync(endOfConversation, cancellationToken);
                return await stepContext.EndDialogAsync();
            }
            else
            {
                return await stepContext.ReplaceDialogAsync(InitialDialogId, _responseManager.GetResponse(SharedResponses.ActionEnded), cancellationToken);
            }
        }

        private async Task LogUserOut(DialogContext dc)
        {
            IUserTokenProvider tokenProvider;
            var supported = dc.Context.Adapter is IUserTokenProvider;
            if (supported)
            {
                tokenProvider = (IUserTokenProvider)dc.Context.Adapter;

                // Sign out user
                var tokens = await tokenProvider.GetTokenStatusAsync(dc.Context, dc.Context.Activity.From.Id);
                foreach (var token in tokens)
                {
                    await tokenProvider.SignOutUserAsync(dc.Context, token.ConnectionName);
                }

                // Cancel all active dialogs
                await dc.CancelAllDialogsAsync();
            }
            else
            {
                throw new InvalidOperationException("OAuthPrompt.SignOutUser(): not supported by the current adapter");
            }
        }

        private async Task<DialogTurnResult> ProcessAction<T>(ITSMLuis.Intent intent, string dialogId, WaterfallStepContext stepContext, CancellationToken cancellationToken)
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

            return await stepContext.BeginDialogAsync(dialogId, null, cancellationToken);
        }
    }
}