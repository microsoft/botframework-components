// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using BingSearchSkill.Models;
using BingSearchSkill.Models.Actions;
using BingSearchSkill.Responses.Main;
using BingSearchSkill.Services;
using BingSearchSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using SkillServiceLibrary.Utilities;

namespace BingSearchSkill.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly BotSettings _settings;
        private readonly BotServices _services;
        private readonly LocaleTemplateManager _templateManager;
        private readonly IStatePropertyAccessor<SkillState> _stateAccessor;
        private readonly Dialog _searchDialog;

        public MainDialog(
            IServiceProvider serviceProvider)
            : base(nameof(MainDialog))
        {
            _settings = serviceProvider.GetService<BotSettings>();
            _services = serviceProvider.GetService<BotServices>();
            _templateManager = serviceProvider.GetService<LocaleTemplateManager>();

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
            _searchDialog = serviceProvider.GetService<SearchDialog>() ?? throw new ArgumentNullException(nameof(SearchDialog));
            AddDialog(_searchDialog);
        }

        // Runs when the dialog is started.
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
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
            if (innerDc.Context.Activity.Type == ActivityTypes.Message)
            {
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
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on General model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("General", out var generalLuisService);
                if (generalLuisService == null)
                {
                    throw new Exception("The general LUIS Model could not be found in your Bot Services configuration.");
                }

                var generalResult = await generalLuisService.RecognizeAsync<General>(innerDc.Context, cancellationToken);
                (var generalIntent, var generalScore) = generalResult.TopIntent();

                if (generalScore > 0.5)
                {
                    switch (generalIntent)
                    {
                        case General.Intent.Cancel:
                            {
                                await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivity(MainResponses.CancelMessage));
                                await innerDc.CancelAllDialogsAsync();
                                if (innerDc.Context.IsSkill())
                                {
                                    var state = await _stateAccessor.GetAsync(innerDc.Context, () => new SkillState());
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
                                await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivity(MainResponses.HelpMessage));
                                await innerDc.RepromptDialogAsync();
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
                return await stepContext.NextAsync();
            }
            else
            {
                // If bot is in local mode, prompt with intro or continuation message
                var prompt = stepContext.Options as Activity ?? _templateManager.GenerateActivity(MainResponses.FirstPromptMessage);
                var state = await _stateAccessor.GetAsync(stepContext.Context, () => new SkillState());
                var activity = stepContext.Context.Activity;
                if (activity.Type == ActivityTypes.ConversationUpdate)
                {
                    prompt = _templateManager.GenerateActivity(MainResponses.WelcomeMessage);
                }

                var promptOptions = new PromptOptions
                {
                    Prompt = prompt
                };

                return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
            }
        }

        // Handles routing to additional dialogs logic.
        private async Task<DialogTurnResult> RouteStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var a = stepContext.Context.Activity;
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new SkillState());
            state.IsAction = false;

            if (a.Type == ActivityTypes.Message && !string.IsNullOrEmpty(a.Text))
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("BingSearchSkill", out var luisService);
                if (luisService == null)
                {
                    throw new Exception("The specified LUIS Model could not be found in your Bot Services configuration.");
                }
                else
                {
                    var result = await luisService.RecognizeAsync<BingSearchSkillLuis>(stepContext.Context, CancellationToken.None);
                    var intent = result?.TopIntent().intent;

                    switch (intent)
                    {
                        case BingSearchSkillLuis.Intent.GetCelebrityInfo:
                        case BingSearchSkillLuis.Intent.SearchMovieInfo:
                        case BingSearchSkillLuis.Intent.None:
                            {
                                return await stepContext.BeginDialogAsync(nameof(SearchDialog));
                            }

                        default:
                            {
                                // intent was identified but not yet implemented
                                await stepContext.Context.SendActivityAsync(_templateManager.GenerateActivity(MainResponses.FeatureNotAvailable));
                                break;
                            }
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
                        case "BingSearch":
                            {
                                KeywordSearchInfo actionData = null;

                                var eventValue = a.Value as JObject;
                                if (eventValue != null)
                                {
                                    actionData = eventValue.ToObject<KeywordSearchInfo>();
                                    await DigestActionInput(stepContext, actionData);
                                }

                                state.IsAction = true;
                                return await stepContext.BeginDialogAsync(nameof(SearchDialog));
                            }

                        default:

                            // todo: move the response to lg
                            await stepContext.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown Event '{eventActivity.Name ?? "undefined"}' was received but not processed."));

                            break;
                    }
                }
            }

            // If activity was unhandled, flow should continue to next step
            return await stepContext.NextAsync();
        }

        private async Task DigestActionInput(DialogContext dc, KeywordSearchInfo request)
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new SkillState());
            state.SearchEntityName = request.Keyword;
        }

        // Handles conversation cleanup.
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Context.IsSkill())
            {
                return await stepContext.EndDialogAsync(stepContext.Result, cancellationToken);
            }
            else
            {
                return await stepContext.ReplaceDialogAsync(this.Id, _templateManager.GenerateActivity(MainResponses.CompletedMessage), cancellationToken);
            }
        }
    }
}