// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using NewsSkill.Models;
using NewsSkill.Responses.Main;
using NewsSkill.Services;
using SkillServiceLibrary.Utilities;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using NewsSkill.Models.Action;
using Newtonsoft.Json.Linq;
using NewsSkill.Responses;

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
        private LocaleTemplateEngineManager _localeTemplateEngineManager;
        //private MainResponses _responder = new MainResponses();

        public MainDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient,
            LocaleTemplateEngineManager localeTemplateEngineManager)
            : base(nameof(MainDialog))
        {
            _settings = serviceProvider.GetService<BotSettings>();
            _services = serviceProvider.GetService<BotServices>();
            //_responseManager = serviceProvider.GetService<ResponseManager>();
            _localeTemplateEngineManager = localeTemplateEngineManager;
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
                var generalResult = innerDc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResult);
                (var generalIntent, var generalScore) = generalResult.TopIntent();

                if (generalScore > 0.5)
                {
                    switch (generalIntent)
                    {
                        case General.Intent.Cancel:
                            {
                                await innerDc.Context.SendActivityAsync(_localeTemplateEngineManager.GenerateActivityForLocale(MainStrings.CANCELLED));
                                //await _responder.ReplyWith(innerDc.Context, MainResponses.Cancelled);
                                await innerDc.CancelAllDialogsAsync();
                                await innerDc.BeginDialogAsync(InitialDialogId);
                                interrupted = true;
                                break;
                            }

                        case General.Intent.Help:
                            {
                                await innerDc.Context.SendActivityAsync(HeroCardResponses.SendHelpCard(innerDc.Context, _localeTemplateEngineManager));
                                //await _responder.ReplyWith(innerDc.Context, MainResponses.Help);
                                await innerDc.RepromptDialogAsync();
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
                var prompt = stepContext.Options as Activity ?? HeroCardResponses.SendIntroCard(stepContext.Context, _localeTemplateEngineManager);
                var state = await _stateAccessor.GetAsync(stepContext.Context, () => new NewsSkillState());
                var activity = stepContext.Context.Activity;
                if (activity.Type == ActivityTypes.ConversationUpdate)
                {
                    prompt = HeroCardResponses.SendIntroCard(stepContext.Context, _localeTemplateEngineManager);
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
                            await stepContext.Context.SendActivityAsync(_localeTemplateEngineManager.GenerateActivityForLocale(MainStrings.CONFUSED));
                            //await _responder.ReplyWith(stepContext.Context, MainResponses.Confused);
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

                                return await stepContext.BeginDialogAsync(nameof(TrendingArticlesDialog), new NewsSkillOptionBase() { IsAction = true });
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

                                return await stepContext.BeginDialogAsync(nameof(FavoriteTopicsDialog), new NewsSkillOptionBase() { IsAction = true });
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

                                return await stepContext.BeginDialogAsync(nameof(FindArticlesDialog), new NewsSkillOptionBase() { IsAction = true });
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
                return await stepContext.ReplaceDialogAsync(InitialDialogId, _localeTemplateEngineManager.GenerateActivityForLocale(MainStrings.COMPLETED), cancellationToken);
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
            convState.LuisResult = new NewsLuis() { Entities = new NewsLuis._Entities() { topic = new string[] { request.Query }, site = new string[] { request.Site } } };
        }
    }
}