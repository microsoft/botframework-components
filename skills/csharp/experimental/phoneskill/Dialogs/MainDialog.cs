// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using PhoneSkill.Models;
using PhoneSkill.Models.Actions;
using PhoneSkill.Responses.Main;
using PhoneSkill.Responses.Shared;
using PhoneSkill.Services;
using PhoneSkill.Services.Luis;
using PhoneSkill.Utilities;
using SkillServiceLibrary.Utilities;

namespace PhoneSkill.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly OutgoingCallDialog outgoingCallDialog;
        private readonly BotSettings _settings;
        private readonly BotServices _services;
        private readonly LocaleTemplateManager _templateManager;
        private readonly IStatePropertyAccessor<PhoneSkillState> _stateAccessor;

        public MainDialog(
            IServiceProvider serviceProvider)
             : base(nameof(MainDialog))
        {
            _settings = serviceProvider.GetService<BotSettings>();
            _services = serviceProvider.GetService<BotServices>();
            _templateManager = serviceProvider.GetService<LocaleTemplateManager>();

            // Create conversation state properties
            var conversationState = serviceProvider.GetService<ConversationState>();
            _stateAccessor = conversationState.CreateProperty<PhoneSkillState>(nameof(PhoneSkillState));

            var steps = new WaterfallStep[]
            {
                IntroStepAsync,
                RouteStepAsync,
                FinalStepAsync,
            };

            AddDialog(new WaterfallDialog(nameof(MainDialog), steps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            InitialDialogId = nameof(MainDialog);

            // register dialog
            this.outgoingCallDialog = serviceProvider.GetService<OutgoingCallDialog>() ?? throw new ArgumentNullException(nameof(OutgoingCallDialog));
            AddDialog(outgoingCallDialog ?? throw new ArgumentNullException(nameof(outgoingCallDialog)));
        }

        // Runs when the dialog is started.
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(innerDc.Context.Activity.Text))
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("phone", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<PhoneLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.PhoneLuisResultKey, skillResult);
                }
                else
                {
                    throw new Exception("The skill LUIS Model could not be found in your Bot Services configuration.");
                }

                // Run LUIS recognition on General model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("general", out var generalLuisService);
                if (generalLuisService != null)
                {
                    var generalResult = await generalLuisService.RecognizeAsync<General>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.GeneralLuisResultKey, generalResult);
                }
                else
                {
                    throw new Exception("The general LUIS Model could not be found in your Bot Services configuration.");
                }

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
                localizedServices.LuisServices.TryGetValue("phone", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<PhoneLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.PhoneLuisResultKey, skillResult);
                }
                else
                {
                    throw new Exception("The skill LUIS Model could not be found in your Bot Services configuration.");
                }

                // Run LUIS recognition on General model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("general", out var generalLuisService);
                if (generalLuisService != null)
                {
                    var generalResult = await generalLuisService.RecognizeAsync<General>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.GeneralLuisResultKey, generalResult);
                }
                else
                {
                    throw new Exception("The general LUIS Model could not be found in your Bot Services configuration.");
                }

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
                var state = await _stateAccessor.GetAsync(innerDc.Context, () => new PhoneSkillState());

                // Get connected LUIS result from turn state.
                var generalResult = innerDc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                (var generalIntent, var generalScore) = generalResult.TopIntent();

                if (generalScore > 0.5)
                {
                    switch (generalIntent)
                    {
                        case General.Intent.Cancel:
                        case General.Intent.StartOver:
                            {
                                await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivity(PhoneMainResponses.CancelMessage), cancellationToken);
                                await innerDc.CancelAllDialogsAsync(cancellationToken);
                                if (innerDc.Context.IsSkill())
                                {
                                    interrupted = await innerDc.EndDialogAsync(state.IsAction ? new ActionResult { ActionSuccess = false } : null, cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    interrupted = await innerDc.BeginDialogAsync(InitialDialogId, cancellationToken: cancellationToken);
                                }

                                break;
                            }

                        case General.Intent.Help:
                            {
                                await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivity(PhoneMainResponses.HelpMessage), cancellationToken);
                                await innerDc.RepromptDialogAsync(cancellationToken);
                                interrupted = EndOfTurn;
                                break;
                            }

                        case General.Intent.Logout:
                            {
                                await OnLogoutAsync(innerDc, cancellationToken);

                                await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivity(PhoneMainResponses.LogOut), cancellationToken);
                                await innerDc.CancelAllDialogsAsync(cancellationToken);
                                if (innerDc.Context.IsSkill())
                                {
                                    interrupted = await innerDc.EndDialogAsync(state.IsAction ? new ActionResult() { ActionSuccess = false } : null, cancellationToken: cancellationToken);
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
            else
            {
                // If bot is in local mode, prompt with intro or continuation message
                var promptOptions = new PromptOptions
                {
                    Prompt = stepContext.Options as Activity ?? _templateManager.GenerateActivity(PhoneMainResponses.FirstPromptMessage)
                };

                if (stepContext.Context.Activity.Type == ActivityTypes.ConversationUpdate)
                {
                    promptOptions.Prompt = _templateManager.GenerateActivity(PhoneMainResponses.WelcomeMessage);
                }

                return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
            }
        }

        // Handles routing to additional dialogs logic.
        private async Task<DialogTurnResult> RouteStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var a = stepContext.Context.Activity;
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new PhoneSkillState(), cancellationToken: cancellationToken);
            state.IsAction = false;

            if (a.Type == ActivityTypes.Message && !string.IsNullOrEmpty(a.Text))
            {
                // Get connected LUIS result from turn state.
                var result = stepContext.Context.TurnState.Get<PhoneLuis>(StateProperties.PhoneLuisResultKey);
                var intent = result?.TopIntent().intent;

                // switch on general intents
                switch (intent)
                {
                    case PhoneLuis.Intent.OutgoingCall:
                        {
                            return await stepContext.BeginDialogAsync(nameof(OutgoingCallDialog), cancellationToken: cancellationToken);
                        }

                    case PhoneLuis.Intent.None:
                        {
                            // No intent was identified, send confused message
                            await stepContext.Context.SendActivityAsync(_templateManager.GenerateActivity(PhoneSharedResponses.DidntUnderstandMessage), cancellationToken);
                            break;
                        }

                    default:
                        {
                            // intent was identified but not yet implemented
                            await stepContext.Context.SendActivityAsync(_templateManager.GenerateActivity(PhoneMainResponses.FeatureNotAvailable));
                            break;
                        }
                }
            }
            else if (a.Type == ActivityTypes.Event)
            {
                // Handle skill actions here
                var eventActivity = a.AsEventActivity();

                switch (eventActivity.Name)
                {
                    case Events.SkillBeginEvent:
                        {
                            if (eventActivity.Value is Dictionary<string, object> userData)
                            {
                                // Capture user data from event if needed
                            }

                            break;
                        }

                    case Events.TokenResponseEvent:
                        {
                            // Auth dialog completion
                            var result = await stepContext.ContinueDialogAsync(cancellationToken);

                            // If the dialog completed when we sent the token, end the skill conversation
                            if (result.Status != DialogTurnStatus.Waiting)
                            {
                                var response = stepContext.Context.Activity.CreateReply();
                                response.Type = ActivityTypes.EndOfConversation;

                                await stepContext.Context.SendActivityAsync(response, cancellationToken);
                            }

                            break;
                        }

                    case Events.OutgoingCallEvent:
                        {
                            OutgoingCallRequest actionData = null;

                            var eventValue = a.Value as JObject;
                            if (eventValue != null)
                            {
                                actionData = eventValue.ToObject<OutgoingCallRequest>();
                                if (!string.IsNullOrEmpty(actionData.ContactPerson) ||
                                    !string.IsNullOrEmpty(actionData.PhoneNumber))
                                {
                                    await DigestActionInput(stepContext, actionData);
                                }
                            }

                            state.IsAction = true;

                            return await stepContext.BeginDialogAsync(nameof(OutgoingCallDialog), cancellationToken: cancellationToken);
                        }

                    default:
                        await stepContext.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown Event '{eventActivity.Name ?? "undefined"}' was received but not processed."), cancellationToken);

                        break;
                }
            }

            // If activity was unhandled, flow should continue to next step
            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }

        // Handles conversation cleanup.
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new PhoneSkillState(), cancellationToken: cancellationToken);

            if (stepContext.Context.IsSkill())
            {
                var result = stepContext.Result;

                if (state.IsAction && result as ActionResult == null)
                {
                    result = new ActionResult() { ActionSuccess = false };
                }

                state.Clear();
                return await stepContext.EndDialogAsync(result, cancellationToken);
            }
            else
            {
                state.Clear();
                return await stepContext.ReplaceDialogAsync(InitialDialogId, _templateManager.GenerateActivity(PhoneMainResponses.CompletedMessage), cancellationToken: cancellationToken);
            }
        }

        private async Task DigestActionInput(DialogContext dc, OutgoingCallRequest request)
        {
            var state = await _stateAccessor.GetAsync(dc.Context);

            // generate the luis result based on event input
            state.LuisResult = new PhoneLuis
            {
                Text = request.ContactPerson,
                Intents = new Dictionary<PhoneLuis.Intent, IntentScore>()
            };
            state.LuisResult.Intents.Add(PhoneLuis.Intent.OutgoingCall, new IntentScore() { Score = 0.9 });
            state.LuisResult.Entities = new PhoneLuis._Entities
            {
                _instance = new PhoneLuis._Entities._Instance(),

                contactName = string.IsNullOrEmpty(request.ContactPerson) ? null : new string[] { request.ContactPerson },
                phoneNumber = string.IsNullOrEmpty(request.PhoneNumber) ? null : new string[] { request.PhoneNumber }
            };

            state.LuisResult.Entities._instance.contactName = string.IsNullOrEmpty(request.ContactPerson) ? null : new[]
                {
                    new InstanceData
                    {
                        StartIndex = 0,
                        EndIndex = request.ContactPerson.Length,
                        Text = request.ContactPerson
                    }
                };

            state.LuisResult.Entities._instance.phoneNumber = string.IsNullOrEmpty(request.PhoneNumber) ? null : new[]
                {
                    new InstanceData
                    {
                        StartIndex = 0,
                        EndIndex = request.PhoneNumber.Length,
                        Text = request.PhoneNumber
                    }
                };

            // save to turn context
            dc.Context.TurnState.Add(StateProperties.PhoneLuisResultKey, state.LuisResult);
        }

        private async Task OnLogoutAsync(DialogContext dc, CancellationToken cancellationToken)
        {
            BotFrameworkAdapter adapter;
            var supported = dc.Context.Adapter is BotFrameworkAdapter;
            if (!supported)
            {
                throw new InvalidOperationException("OAuthPrompt.SignOutUser(): not supported by the current adapter");
            }
            else
            {
                adapter = (BotFrameworkAdapter)dc.Context.Adapter;
            }

            // Sign out user
            var tokens = await adapter.GetTokenStatusAsync(dc.Context, dc.Context.Activity.From.Id, cancellationToken: cancellationToken);
            foreach (var token in tokens)
            {
                await adapter.SignOutUserAsync(dc.Context, token.ConnectionName, cancellationToken: cancellationToken);
            }

            await dc.CancelAllDialogsAsync(cancellationToken);

            await outgoingCallDialog.OnLogoutAsync(dc, cancellationToken);
        }

        private class Events
        {
            public const string TokenResponseEvent = "tokens/response";
            public const string SkillBeginEvent = "skillBegin";
            public const string OutgoingCallEvent = "OutgoingCall";
        }
    }
}