// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Spatial;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using SkillServiceLibrary.Utilities;
using WeatherSkill.Models;
using WeatherSkill.Models.Action;
using WeatherSkill.Responses.Main;
using WeatherSkill.Responses.Shared;
using WeatherSkill.Services;
using WeatherSkill.Utilities;

namespace WeatherSkill.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly BotSettings _settings;
        private readonly BotServices _services;
        private readonly LocaleTemplateManager _localeTemplateManager;
        private readonly IStatePropertyAccessor<SkillState> _stateAccessor;

        public MainDialog(
            IServiceProvider serviceProvider)
            : base(nameof(MainDialog))
        {
            _settings = serviceProvider.GetService<BotSettings>();
            _services = serviceProvider.GetService<BotServices>();
            _localeTemplateManager = serviceProvider.GetService<LocaleTemplateManager>();

            // Create conversation state properties
            var conversationState = serviceProvider.GetService<ConversationState>();
            _stateAccessor = conversationState.CreateProperty<SkillState>(nameof(SkillState));

            // Initialize state accessor
            var userState = serviceProvider.GetService<UserState>();

            var steps = new WaterfallStep[]
            {
                IntroStepAsync,
                RouteStepAsync,
                FinalStepAsync,
            };

            AddDialog(new WaterfallDialog(nameof(MainDialog), steps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            InitialDialogId = nameof(MainDialog);

            // RegisterDialogs
            AddDialog(serviceProvider.GetService<ForecastDialog>() ?? throw new ArgumentNullException(nameof(ForecastDialog)));
        }

        // Runs when the dialog is started.
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(innerDc.Context.Activity.Text))
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("WeatherSkill", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<WeatherSkillLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.WeatherLuisResult, skillResult);
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
                    innerDc.Context.TurnState.Add(StateProperties.GeneralLuisResult, generalResult);
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
                localizedServices.LuisServices.TryGetValue("WeatherSkill", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<WeatherSkillLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.WeatherLuisResult, skillResult);
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
                    innerDc.Context.TurnState.Add(StateProperties.GeneralLuisResult, generalResult);
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
                var generalResult = innerDc.Context.TurnState.Get<GeneralLuis>(StateProperties.GeneralLuisResult);
                (var generalIntent, var generalScore) = generalResult.TopIntent();

                if (generalScore > 0.5)
                {
                    switch (generalIntent)
                    {
                        case GeneralLuis.Intent.Cancel:
                            {
                                await innerDc.Context.SendActivityAsync(_localeTemplateManager.GenerateActivity(MainResponses.CancelMessage), cancellationToken);
                                await innerDc.CancelAllDialogsAsync(cancellationToken);
                                if (innerDc.Context.IsSkill())
                                {
                                    var state = await _stateAccessor.GetAsync(innerDc.Context, () => new SkillState(), cancellationToken);
                                    interrupted = await innerDc.EndDialogAsync(state.IsAction ? new ActionResult { ActionSuccess = false } : null, cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    interrupted = await innerDc.BeginDialogAsync(InitialDialogId, cancellationToken: cancellationToken);
                                }

                                break;
                            }

                        case GeneralLuis.Intent.Help:
                            {
                                await innerDc.Context.SendActivityAsync(_localeTemplateManager.GenerateActivity(MainResponses.HelpMessage), cancellationToken);
                                await innerDc.RepromptDialogAsync(cancellationToken);
                                interrupted = EndOfTurn;
                                break;
                            }

                        case GeneralLuis.Intent.Logout:
                            {
                                await OnLogoutAsync(innerDc, cancellationToken);
                                await innerDc.Context.SendActivityAsync(_localeTemplateManager.GenerateActivity(MainResponses.LogOut), cancellationToken);
                                await innerDc.CancelAllDialogsAsync(cancellationToken);
                                if (innerDc.Context.IsSkill())
                                {
                                    interrupted = await innerDc.EndDialogAsync(cancellationToken: cancellationToken);
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
                Prompt = stepContext.Options as Activity ?? _localeTemplateManager.GenerateActivity(MainResponses.FirstPromptMessage)
            };

            if (stepContext.Context.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                promptOptions.Prompt = _localeTemplateManager.GenerateActivity(MainResponses.WelcomeMessage);
            }

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
                var result = stepContext.Context.TurnState.Get<WeatherSkillLuis>(StateProperties.WeatherLuisResult);
                var intent = result?.TopIntent().intent;
                switch (intent)
                {
                    case WeatherSkillLuis.Intent.CheckWeatherValue:
                        {
                            return await stepContext.BeginDialogAsync(nameof(ForecastDialog), cancellationToken: cancellationToken);
                        }

                    case WeatherSkillLuis.Intent.None:
                        {
                            // No intent was identified, send confused message
                            await stepContext.Context.SendActivityAsync(_localeTemplateManager.GenerateActivity(SharedResponses.DidntUnderstandMessage), cancellationToken);
                            break;
                        }

                    default:
                        {
                            // intent was identified but not yet implemented
                            await stepContext.Context.SendActivityAsync(_localeTemplateManager.GenerateActivity(MainResponses.FeatureNotAvailable), cancellationToken);
                            break;
                        }
                }
            }
            else if (activity.Type == ActivityTypes.Event)
            {
                // Handle skill actions here
                var eventActivity = activity.AsEventActivity();
                if (!string.IsNullOrEmpty(eventActivity.Name))
                {
                    switch (eventActivity.Name)
                    {
                        // Each Action in the Manifest will have an associated Name which will be on incoming Event activities
                        case ActionNames.WeatherForecast:
                            {
                                state.IsAction = true;

                                LocationInfo actionData = null;
                                var eventValue = activity.Value as JObject;
                                if (eventValue != null)
                                {
                                    actionData = eventValue.ToObject<LocationInfo>();
                                    await DigestActionInputAsync(stepContext, actionData, cancellationToken);
                                }

                                return await stepContext.BeginDialogAsync(nameof(ForecastDialog), cancellationToken: cancellationToken);
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
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new SkillState(), cancellationToken);

            if (stepContext.Context.IsSkill())
            {
                var result = stepContext.Result;

                if (state.IsAction && result as ActionResult == null)
                {
                    result = new ActionResult() { ActionSuccess = false };
                }

                state.Clear();
                return await stepContext.EndDialogAsync(result, cancellationToken: cancellationToken);
            }
            else
            {
                state.Clear();
                return await stepContext.ReplaceDialogAsync(InitialDialogId, _localeTemplateManager.GenerateActivity(MainResponses.CompletedMessage), cancellationToken);
            }
        }

        private async Task<InterruptionAction> OnLogoutAsync(DialogContext dc, CancellationToken cancellationToken)
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

            await dc.CancelAllDialogsAsync(cancellationToken);

            // Sign out user
            var tokens = await adapter.GetTokenStatusAsync(dc.Context, dc.Context.Activity.From.Id);
            foreach (var token in tokens)
            {
                await adapter.SignOutUserAsync(dc.Context, token.ConnectionName, cancellationToken: cancellationToken);
            }

            await dc.Context.SendActivityAsync(_localeTemplateManager.GenerateActivity(MainResponses.LogOut), cancellationToken);

            return InterruptionAction.End;
        }

        private async Task PopulateStateFromSkillContext(ITurnContext context, CancellationToken cancellationToken)
        {
            // Populating local state with data passed through semanticAction out of Activity
            var activity = context.Activity;
            var semanticAction = activity.SemanticAction;
            if (semanticAction != null && semanticAction.Entities.ContainsKey("location"))
            {
                var location = semanticAction.Entities["location"];
                var locationObj = location.Properties["location"].ToString();

                var coords = locationObj.Split(',');
                if (coords.Length == 2 && double.TryParse(coords[0], out var lat) && double.TryParse(coords[1], out var lng))
                {
                    var state = await _stateAccessor.GetAsync(context, () => new SkillState(), cancellationToken);
                    state.Latitude = lat;
                    state.Longitude = lng;
                }
                else
                {
                    // In case name has ','
                    var state = await _stateAccessor.GetAsync(context, () => new SkillState(), cancellationToken);
                    state.Geography = locationObj;
                }
            }
        }

        private async Task DigestActionInputAsync(DialogContext dc, LocationInfo locationInfo, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new SkillState(), cancellationToken);
            var location = locationInfo.Location;
            var coords = location.Split(',');
            if (coords.Length == 2 && double.TryParse(coords[0], out var lat) && double.TryParse(coords[1], out var lng))
            {
                state.Latitude = lat;
                state.Longitude = lng;
            }
            else
            {
                state.Geography = location;
            }
        }
    }
}