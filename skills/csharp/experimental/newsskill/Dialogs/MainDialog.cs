// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using NewsSkill.Models;
using NewsSkill.Models.Action;
using NewsSkill.Responses;
using NewsSkill.Responses.Main;
using NewsSkill.Services;
using Newtonsoft.Json.Linq;
using SkillServiceLibrary.Utilities;

namespace NewsSkill.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private IStatePropertyAccessor<NewsSkillState> _stateAccessor;
        private IStatePropertyAccessor<NewsSkillUserState> _userStateAccessor;
        private Dialog _findArticlesDialog;
        private Dialog _trendingArticlesDialog;
        private Dialog _favoriteTopicsDialog;
        private LocaleTemplateManager _templateManager;

        public MainDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient,
            LocaleTemplateManager templateManager)
            : base(nameof(MainDialog))
        {
            _settings = serviceProvider.GetService<BotSettings>();
            _services = serviceProvider.GetService<BotServices>();
            _templateManager = templateManager;
            TelemetryClient = telemetryClient;

            // Create conversation state properties
            var conversationState = serviceProvider.GetService<ConversationState>();
            _stateAccessor = conversationState.CreateProperty<NewsSkillState>(nameof(NewsSkillState));
            var userState = serviceProvider.GetService<UserState>();
            _userStateAccessor = userState.CreateProperty<NewsSkillUserState>(nameof(NewsSkillUserState));

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
            _findArticlesDialog = serviceProvider.GetService<FindArticlesDialog>() ?? throw new ArgumentNullException(nameof(FindArticlesDialog));
            _trendingArticlesDialog = serviceProvider.GetService<TrendingArticlesDialog>() ?? throw new ArgumentNullException(nameof(FindArticlesDialog));
            _favoriteTopicsDialog = serviceProvider.GetService<FavoriteTopicsDialog>() ?? throw new ArgumentNullException(nameof(FindArticlesDialog));
            AddDialog(_findArticlesDialog);
            AddDialog(_trendingArticlesDialog);
            AddDialog(_favoriteTopicsDialog);
        }

        // Runs when the dialog is started.
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            var activity = innerDc.Context.Activity;

            if (activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(activity.Text))
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                var skillResult = await localizedServices.LuisServices["News"].RecognizeAsync<NewsLuis>(innerDc.Context, cancellationToken);
                innerDc.Context.TurnState.Add(StateProperties.SkillLuisResult, skillResult);

                // Run LUIS recognition on General model and store result in turn state.
                var generalResult = await localizedServices.LuisServices["General"].RecognizeAsync<General>(innerDc.Context, cancellationToken);
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
            var activity = innerDc.Context.Activity;

            if (activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(activity.Text))
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                var skillResult = await localizedServices.LuisServices["News"].RecognizeAsync<NewsLuis>(innerDc.Context, cancellationToken);
                innerDc.Context.TurnState.Add(StateProperties.SkillLuisResult, skillResult);

                // Run LUIS recognition on General model and store result in turn state.
                var generalResult = await localizedServices.LuisServices["General"].RecognizeAsync<General>(innerDc.Context, cancellationToken);
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
                var generalResult = innerDc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResult);
                (var generalIntent, var generalScore) = generalResult.TopIntent();

                if (generalScore > 0.5)
                {
                    switch (generalIntent)
                    {
                        case General.Intent.Cancel:
                            {
                                await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale(MainStrings.CANCELLED));
                                await innerDc.CancelAllDialogsAsync();
                                if (innerDc.Context.IsSkill())
                                {
                                    var state = await _stateAccessor.GetAsync(innerDc.Context, () => new NewsSkillState());
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
                                await innerDc.Context.SendActivityAsync(HeroCardResponses.SendHelpCard(innerDc.Context, _templateManager));
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
                var prompt = stepContext.Options as Activity ?? HeroCardResponses.SendIntroCard(stepContext.Context, _templateManager);
                var state = await _stateAccessor.GetAsync(stepContext.Context, () => new NewsSkillState());
                var activity = stepContext.Context.Activity;
                if (activity.Type == ActivityTypes.ConversationUpdate)
                {
                    prompt = HeroCardResponses.SendIntroCard(stepContext.Context, _templateManager);
                }

                var promptOptions = new PromptOptions
                {
                    Prompt = (Activity)prompt
                };

                return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
            }
        }

        // Handles routing to additional dialogs logic.
        private async Task<DialogTurnResult> RouteStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var a = stepContext.Context.Activity;
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new NewsSkillState());
            state.IsAction = false;

            if (a.Type == ActivityTypes.Message && !string.IsNullOrEmpty(a.Text))
            {
                var result = stepContext.Context.TurnState.Get<NewsLuis>(StateProperties.SkillLuisResult);
                state.LuisResult = result;

                var intent = result?.TopIntent().intent;

                // switch on general intents
                switch (intent)
                {
                    case NewsLuis.Intent.TrendingArticles:
                        {
                            // send articles in response
                            return await stepContext.BeginDialogAsync(nameof(TrendingArticlesDialog));
                        }

                    case NewsLuis.Intent.SetFavoriteTopics:
                    case NewsLuis.Intent.ShowFavoriteTopics:
                        {
                            // send favorite news categories
                            return await stepContext.BeginDialogAsync(nameof(FavoriteTopicsDialog));
                        }

                    case NewsLuis.Intent.FindArticles:
                        {
                            // send greeting response
                            return await stepContext.BeginDialogAsync(nameof(FindArticlesDialog));
                        }

                    case NewsLuis.Intent.None:
                        {
                            // No intent was identified, send confused message
                            await stepContext.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale(MainStrings.CONFUSED));
                            break;
                        }

                    default:
                        {
                            // intent was identified but not yet implemented
                            await stepContext.Context.SendActivityAsync("This feature is not yet implemented in this skill.");
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
                        case "GetTrendingArticles":
                            {
                                TrendingArticlesInput actionData = null;

                                var eventValue = a.Value as JObject;
                                if (eventValue != null)
                                {
                                    actionData = eventValue.ToObject<TrendingArticlesInput>();
                                    await DigestActionInput(stepContext, actionData);
                                }

                                state.IsAction = true;
                                return await stepContext.BeginDialogAsync(nameof(TrendingArticlesDialog));
                            }

                        case "GetFavoriteTopics":
                            {
                                FavoriteTopicsInput actionData = null;

                                var eventValue = a.Value as JObject;
                                if (eventValue != null)
                                {
                                    actionData = eventValue.ToObject<FavoriteTopicsInput>();
                                    await DigestActionInput(stepContext, actionData);
                                }

                                state.IsAction = true;
                                return await stepContext.BeginDialogAsync(nameof(FavoriteTopicsDialog));
                            }

                        case "FindArticles":
                            {
                                FindArticlesInput actionData = null;

                                var eventValue = a.Value as JObject;
                                if (eventValue != null)
                                {
                                    actionData = eventValue.ToObject<FindArticlesInput>();
                                    await DigestActionInput(stepContext, actionData);
                                }

                                state.IsAction = true;
                                return await stepContext.BeginDialogAsync(nameof(FindArticlesDialog));
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

        // Handles conversation cleanup.
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Context.IsSkill())
            {
                return await stepContext.EndDialogAsync(stepContext.Result, cancellationToken);
            }
            else
            {
                return await stepContext.ReplaceDialogAsync(InitialDialogId, _templateManager.GenerateActivityForLocale(MainStrings.COMPLETED), cancellationToken);
            }
        }

        private async Task DigestActionInput(DialogContext dc, TrendingArticlesInput request)
        {
            var userState = await _userStateAccessor.GetAsync(dc.Context, () => new NewsSkillUserState());
            userState.Market = request.Market;
        }

        private async Task DigestActionInput(DialogContext dc, FavoriteTopicsInput request)
        {
            var userState = await _userStateAccessor.GetAsync(dc.Context, () => new NewsSkillUserState());
            userState.Market = request.Market;
            userState.Category = new FoundChoice() { Value = request.Category };
        }

        private async Task DigestActionInput(DialogContext dc, FindArticlesInput request)
        {
            var userState = await _userStateAccessor.GetAsync(dc.Context, () => new NewsSkillUserState());
            userState.Market = request.Market;
            var convState = await _stateAccessor.GetAsync(dc.Context, () => new NewsSkillState());
            var newsLuis = new NewsLuis() { Entities = new NewsLuis._Entities() };
            if (request.Query != null)
            {
                newsLuis.Entities.topic = new string[] { request.Query };
            }

            if (request.Site != null)
            {
                newsLuis.Entities.site = new string[] { request.Site };
            }

            convState.LuisResult = newsLuis;
        }
    }
}