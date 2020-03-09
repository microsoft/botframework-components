// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using MusicSkill.Models;
using MusicSkill.Models.ActionInfos;
using MusicSkill.Responses.Main;
using MusicSkill.Responses.Shared;
using MusicSkill.Services;
using Newtonsoft.Json.Linq;
using SkillServiceLibrary.Utilities;

namespace MusicSkill.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private ResponseManager _responseManager;
        private IStatePropertyAccessor<SkillState> _stateAccessor;
        private Dialog _playMusicDialog;

        public MainDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog))
        {
            _settings = serviceProvider.GetService<BotSettings>();
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
            _playMusicDialog = serviceProvider.GetService<PlayMusicDialog>() ?? throw new ArgumentNullException(nameof(PlayMusicDialog));
            AddDialog(_playMusicDialog);
        }

        // Runs when the dialog is started.
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(innerDc.Context.Activity.Text))
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("MusicSkill", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<MusicSkillLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState[StateProperties.MusicLuisResultKey] = skillResult;
                }
                else
                {
                    throw new Exception("The skill LUIS Model could not be found in your Bot Services configuration.");
                }

                // Run LUIS recognition on General model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("General", out var generalLuisService);
                if (generalLuisService != null)
                {
                    var generalResult = await generalLuisService.RecognizeAsync<GeneralLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState[StateProperties.GeneralLuisResultKey] = generalResult;
                }
                else
                {
                    throw new Exception("The general LUIS Model could not be found in your Bot Services configuration.");
                }

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
                localizedServices.LuisServices.TryGetValue("MusicSkill", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<MusicSkillLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState[StateProperties.MusicLuisResultKey] = skillResult;
                }
                else
                {
                    throw new Exception("The skill LUIS Model could not be found in your Bot Services configuration.");
                }

                // Run LUIS recognition on General model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("General", out var generalLuisService);
                if (generalLuisService != null)
                {
                    var generalResult = await generalLuisService.RecognizeAsync<GeneralLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState[StateProperties.GeneralLuisResultKey] = generalResult;
                }
                else
                {
                    throw new Exception("The general LUIS Model could not be found in your Bot Services configuration.");
                }

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
                var state = await _stateAccessor.GetAsync(innerDc.Context, () => new SkillState());
                var generalResult = innerDc.Context.TurnState.Get<GeneralLuis>(StateProperties.GeneralLuisResultKey);
                (var generalIntent, var generalScore) = generalResult.TopIntent();

                if (generalScore > 0.5)
                {
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
                                await LogUserOut(innerDc);

                                await innerDc.Context.SendActivityAsync(_responseManager.GetResponse(MainResponses.LogOut));
                                await innerDc.CancelAllDialogsAsync();
                                await innerDc.BeginDialogAsync(InitialDialogId);
                                interrupted = true;
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
                return await stepContext.NextAsync();
            }
            else
            {
                // If bot is in local mode, prompt with intro or continuation message
                var promptOptions = new PromptOptions
                {
                    Prompt = stepContext.Options as Activity ?? _responseManager.GetResponse(MainResponses.FirstPromptMessage)
                };

                if (stepContext.Context.Activity.Type == ActivityTypes.ConversationUpdate)
                {
                    promptOptions.Prompt = _responseManager.GetResponse(MainResponses.WelcomeMessage);
                }

                return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
            }
        }

        // Handles routing to additional dialogs logic.
        private async Task<DialogTurnResult> RouteStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var a = stepContext.Context.Activity;
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new SkillState());

            if (a.Type == ActivityTypes.Message && !string.IsNullOrEmpty(a.Text))
            {
                var skillResult = stepContext.Context.TurnState.Get<MusicSkillLuis>(StateProperties.MusicLuisResultKey);
                var intent = skillResult?.TopIntent().intent;

                // switch on general intents
                switch (intent)
                {
                    case MusicSkillLuis.Intent.PlayMusic:
                        {
                            return await stepContext.BeginDialogAsync(_playMusicDialog.Id);
                        }

                    case MusicSkillLuis.Intent.None:
                        {
                            // No intent was identified, send confused message
                            await stepContext.Context.SendActivityAsync(_responseManager.GetResponse(SharedResponses.DidntUnderstandMessage));
                            break;
                        }

                    default:
                        {
                            // intent was identified but not yet implemented
                            await stepContext.Context.SendActivityAsync(_responseManager.GetResponse(MainResponses.FeatureNotAvailable));
                            break;
                        }
                }
            }
            else if (a.Type == ActivityTypes.Event)
            {
                var ev = a.AsEventActivity();

                switch (ev.Name)
                {
                    case TokenEvents.TokenResponseEventName:
                        {
                            // Auth dialog completion
                            var result = await stepContext.ContinueDialogAsync();

                            // If the dialog completed when we sent the token, end the skill conversation
                            if (result.Status != DialogTurnStatus.Waiting)
                            {
                                var response = stepContext.Context.Activity.CreateReply();
                                response.Type = ActivityTypes.EndOfConversation;

                                await stepContext.Context.SendActivityAsync(response);
                            }

                            break;
                        }

                    case Events.PlayMusic:
                        {
                            SearchInfo actionData = null;
                            if (ev.Value is JObject info)
                            {
                                actionData = info.ToObject<SearchInfo>();
                                actionData.DigestState(state);
                            }

                            return await stepContext.BeginDialogAsync(_playMusicDialog.Id);
                        }

                    default:
                        {
                            await stepContext.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown Event '{ev.Name ?? "undefined"}' was received but not processed."));
                            break;
                        }
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

                await stepContext.Context.SendActivityAsync(endOfConversation, cancellationToken);
                return await stepContext.EndDialogAsync();
            }
            else
            {
                return await stepContext.ReplaceDialogAsync(this.Id, _responseManager.GetResponse(MainResponses.CompletedMessage), cancellationToken);
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

        private class Events
        {
            public const string PlayMusic = "PlayMusic";
        }
    }
}