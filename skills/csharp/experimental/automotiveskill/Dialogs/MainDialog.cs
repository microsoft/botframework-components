// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using AutomotiveSkill.Models;
using AutomotiveSkill.Models.Actions;
using AutomotiveSkill.Responses.Main;
using AutomotiveSkill.Responses.Shared;
using AutomotiveSkill.Services;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using SkillServiceLibrary.Utilities;

namespace AutomotiveSkill.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly BotSettings _settings;
        private readonly BotServices _services;
        private readonly LocaleTemplateManager _templateManager;
        private readonly IStatePropertyAccessor<AutomotiveSkillState> _stateAccessor;
        private readonly Dialog _vehicleSettingsDialog;

        public MainDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog))
        {
            _settings = serviceProvider.GetService<BotSettings>();
            _services = serviceProvider.GetService<BotServices>();
            _templateManager = serviceProvider.GetService<LocaleTemplateManager>();
            TelemetryClient = telemetryClient;

            // Create conversation state properties
            var conversationState = serviceProvider.GetService<ConversationState>();
            _stateAccessor = conversationState.CreateProperty<AutomotiveSkillState>(nameof(AutomotiveSkillState));

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
            _vehicleSettingsDialog = serviceProvider.GetService<VehicleSettingsDialog>() ?? throw new ArgumentNullException(nameof(VehicleSettingsDialog));
            AddDialog(_vehicleSettingsDialog);
        }

        // Runs when the dialog is started.
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(innerDc.Context.Activity.Text))
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("Settings", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<SettingsLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState[StateProperties.SettingsLuisResultKey] = skillResult;
                }
                else
                {
                    throw new Exception("The skill LUIS Model could not be found in your Bot Services configuration.");
                }

                // Run LUIS recognition on General model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("General", out var generalLuisService);
                if (generalLuisService != null)
                {
                    var generalResult = await generalLuisService.RecognizeAsync<General>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState[StateProperties.GeneralLuisResultKey] = generalResult;
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
                localizedServices.LuisServices.TryGetValue("Settings", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<SettingsLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState[StateProperties.SettingsLuisResultKey] = skillResult;
                }
                else
                {
                    throw new Exception("The skill LUIS Model could not be found in your Bot Services configuration.");
                }

                // Run LUIS recognition on General model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("General", out var generalLuisService);
                if (generalLuisService != null)
                {
                    var generalResult = await generalLuisService.RecognizeAsync<General>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState[StateProperties.GeneralLuisResultKey] = generalResult;
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
                // Get connected LUIS result from turn state.
                var state = await _stateAccessor.GetAsync(innerDc.Context, () => new AutomotiveSkillState(), cancellationToken);
                var generalResult = innerDc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                (var generalIntent, var generalScore) = generalResult.TopIntent();

                if (generalScore > 0.5)
                {
                    switch (generalIntent)
                    {
                        case General.Intent.Cancel:
                            {
                                await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale(AutomotiveSkillMainResponses.CancelMessage), cancellationToken);
                                await innerDc.CancelAllDialogsAsync();
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

                        case General.Intent.Help:
                            {
                                await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale(AutomotiveSkillMainResponses.HelpMessage), cancellationToken);
                                await innerDc.RepromptDialogAsync(cancellationToken);
                                interrupted = EndOfTurn;
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
                Prompt = stepContext.Options as Activity ?? _templateManager.GenerateActivityForLocale(AutomotiveSkillMainResponses.FirstPromptMessage)
            };

            if (stepContext.Context.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                promptOptions.Prompt = _templateManager.GenerateActivityForLocale(AutomotiveSkillMainResponses.WelcomeMessage);
            }

            return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
        }

        // Handles routing to additional dialogs logic.
        private async Task<DialogTurnResult> RouteStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var a = stepContext.Context.Activity;
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new AutomotiveSkillState(), cancellationToken);
            state.Clear();

            if (a.Type == ActivityTypes.Message && !string.IsNullOrEmpty(a.Text))
            {
                var skillResult = stepContext.Context.TurnState.Get<SettingsLuis>(StateProperties.SettingsLuisResultKey);
                var intent = skillResult?.TopIntent().intent;
                state.AddRecognizerResult(skillResult);

                // switch on general intents
                switch (intent)
                {
                    case SettingsLuis.Intent.VEHICLE_SETTINGS_CHANGE:
                    case SettingsLuis.Intent.VEHICLE_SETTINGS_DECLARATIVE:
                        {
                            return await stepContext.BeginDialogAsync(nameof(VehicleSettingsDialog), cancellationToken: cancellationToken);
                        }

                    case SettingsLuis.Intent.VEHICLE_SETTINGS_CHECK:
                        {
                            await stepContext.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale(AutomotiveSkillMainResponses.FeatureNotAvailable), cancellationToken);
                            break;
                        }

                    case SettingsLuis.Intent.None:
                        {
                            await stepContext.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale(AutomotiveSkillSharedResponses.DidntUnderstandMessage), cancellationToken);
                            break;
                        }

                    default:
                        {
                            await stepContext.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale(AutomotiveSkillMainResponses.FeatureNotAvailable), cancellationToken);
                            break;
                        }
                }
            }
            else if (a.Type == ActivityTypes.Event)
            {
                // Handle skill actions here
                var eventActivity = a.AsEventActivity();
                if (!string.IsNullOrEmpty(eventActivity.Name))
                {
                    switch (eventActivity.Name)
                    {
                        // Each Action in the Manifest will have an associated Name which will be on incoming Event activities
                        case Events.ChangeVehicleSetting:
                            {
                                state.IsAction = true;
                                SettingInfo actionData = null;
                                var eventValue = a.Value as JObject;
                                if (eventValue != null)
                                {
                                    actionData = eventValue.ToObject<SettingInfo>();
                                    actionData.DigestState(state);
                                }

                                return await stepContext.BeginDialogAsync(nameof(VehicleSettingsDialog), cancellationToken: cancellationToken);
                            }

                        default:
                            {
                                // todo: move the response to lg
                                await stepContext.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown Event '{eventActivity.Name ?? "undefined"}' was received but not processed."), cancellationToken);
                                break;
                            }
                    }
                }
            }

            // If activity was unhandled, flow should continue to next step
            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }

        // Handles conversation cleanup.
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new AutomotiveSkillState(), cancellationToken);

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
                return await stepContext.ReplaceDialogAsync(InitialDialogId, _templateManager.GenerateActivityForLocale(AutomotiveSkillMainResponses.CompletedMessage), cancellationToken);
            }
        }

        private class Events
        {
            public const string ChangeVehicleSetting = "ChangeVehicleSetting";
        }
    }
}
