// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Search.NewsSearch.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using NewsSkill.Models;
using NewsSkill.Models.Action;
using NewsSkill.Responses.Main;
using NewsSkill.Services;

namespace NewsSkill.Dialogs
{
    public class NewsDialogBase : ComponentDialog
    {
        protected const string LuisResultKey = "LuisResult";
        private readonly AzureMapsService _mapsService;

        public NewsDialogBase(
            string dialogId,
            IServiceProvider serviceProvider)
            : base(dialogId)
        {
            Settings = serviceProvider.GetService<BotSettings>();
            Services = serviceProvider.GetService<BotServices>();
            TemplateManager = serviceProvider.GetService<LocaleTemplateManager>();

            var conversationState = serviceProvider.GetService<ConversationState>();
            ConvAccessor = conversationState.CreateProperty<NewsSkillState>(nameof(NewsSkillState));
            var userState = serviceProvider.GetService<UserState>();
            UserAccessor = userState.CreateProperty<NewsSkillUserState>(nameof(NewsSkillUserState));

            var mapsKey = Settings.AzureMapsKey ?? throw new Exception("The AzureMapsKey must be provided to use this dialog. Please provide this key in your Skill Configuration.");
            _mapsService = serviceProvider.GetService<AzureMapsService>();
            _mapsService.InitKeyAsync(mapsKey);
        }

        protected BotSettings Settings { get; }

        protected BotServices Services { get; set; }

        protected IStatePropertyAccessor<NewsSkillState> ConvAccessor { get; set; }

        protected IStatePropertyAccessor<NewsSkillUserState> UserAccessor { get; set; }

        protected LocaleTemplateManager TemplateManager { get; }

        // This method is called by any waterfall step that throws an exception to ensure consistency
        public async Task<Exception> HandleDialogExceptionsAsync(WaterfallStepContext sc, Exception ex, CancellationToken cancellationToken)
        {
            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(MainStrings.ERROR), cancellationToken);

            await sc.CancelAllDialogsAsync(cancellationToken);

            return ex;
        }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await ConvAccessor.GetAsync(dc.Context, cancellationToken: cancellationToken);

            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await ConvAccessor.GetAsync(dc.Context, cancellationToken: cancellationToken);

            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }

        protected async Task<DialogTurnResult> GetMarketAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserAccessor.GetAsync(sc.Context, () => new NewsSkillUserState(), cancellationToken: cancellationToken);
            var convState = await ConvAccessor.GetAsync(sc.Context, () => new NewsSkillState(), cancellationToken: cancellationToken);

            // Check if there's already a location
            if (!string.IsNullOrWhiteSpace(userState.Market))
            {
                return await sc.NextAsync(userState.Market, cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(convState.CurrentCoordinates))
            {
                // make maps service query with location coordinates instead of user input
                return await sc.NextAsync(convState.CurrentCoordinates, cancellationToken);
            }

            // Prompt user for location
            return await sc.PromptAsync(nameof(TextPrompt), new PromptOptions()
            {
                Prompt = TemplateManager.GenerateActivityForLocale(MainStrings.MarketPrompt),
                RetryPrompt = TemplateManager.GenerateActivityForLocale(MainStrings.MarketRetryPrompt)
            });
        }

        protected async Task<DialogTurnResult> SetMarketAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserAccessor.GetAsync(sc.Context, () => new NewsSkillUserState(), cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(userState.Market))
            {
                string countryregion = (string)sc.Result;

                // use AzureMaps API to get country code from country or region input by user
                userState.Market = await _mapsService.GetCountryCodeAsync(countryregion);
            }

            return await sc.NextAsync(cancellationToken: cancellationToken);
        }

        protected async Task<bool> MarketPromptValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var countryregion = promptContext.Recognized.Value;

            // check for valid country code
            var countryCode = await _mapsService.GetCountryCodeAsync(countryregion);
            if (countryCode != null)
            {
                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }

        protected ActionResult GenerateNewsActionResult(IList<NewsArticle> articles, bool actionSuccess)
        {
            var actionResult = new ActionResult()
            {
                NewsList = new List<NewsInfo>(),
                ActionSuccess = actionSuccess
            };

            var newsArticles = articles as List<NewsArticle>;
            foreach (var article in newsArticles)
            {
                var newsInfo = new NewsInfo()
                {
                    Title = article.Name,
                    Subtitle = article.DatePublished,
                    Description = article.Description,
                    ImageUrl = article?.Image?.Thumbnail?.ContentUrl,
                    Url = article.WebSearchUrl
                };
                actionResult.NewsList.Add(newsInfo);
            }

            return actionResult;
        }

        protected ActionResult GenerateNewsActionResult(IList<NewsTopic> articles, bool actionSuccess)
        {
            var actionResult = new ActionResult()
            {
                NewsList = new List<NewsInfo>(),
                ActionSuccess = actionSuccess
            };

            var newsArticles = articles as List<NewsTopic>;
            foreach (var article in newsArticles)
            {
                var newsInfo = new NewsInfo()
                {
                    Title = article.Name,
                    Description = article.Description,
                    ImageUrl = article?.Image?.Url,
                    Url = article.WebSearchUrl
                };
                actionResult.NewsList.Add(newsInfo);
            }

            return actionResult;
        }
    }
}