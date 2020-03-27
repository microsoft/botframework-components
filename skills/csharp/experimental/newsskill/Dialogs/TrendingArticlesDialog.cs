// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using NewsSkill.Models;
using NewsSkill.Responses;
using NewsSkill.Services;

namespace NewsSkill.Dialogs
{
    public class TrendingArticlesDialog : NewsDialogBase
    {
        private NewsClient _client;

        public TrendingArticlesDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            UserState userState,
            AzureMapsService mapsService,
            LocaleTemplateManager templateManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(TrendingArticlesDialog), settings, services, conversationState, userState, mapsService, templateManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var key = settings.BingNewsKey ?? throw new Exception("The BingNewsKey must be provided to use this dialog. Please provide this key in your Skill Configuration.");

            _client = new NewsClient(key);

            var trendingArticles = new WaterfallStep[]
            {
                GetMarket,
                SetMarket,
                ShowArticles,
            };

            AddDialog(new WaterfallDialog(nameof(TrendingArticlesDialog), trendingArticles));
            AddDialog(new TextPrompt(nameof(TextPrompt), MarketPromptValidatorAsync));
        }

        private async Task<DialogTurnResult> ShowArticles(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserAccessor.GetAsync(sc.Context, () => new NewsSkillUserState());

            var articles = await _client.GetTrendingNews(userState.Market);
            await sc.Context.SendActivityAsync(HeroCardResponses.ShowTrendingCards(sc.Context, templateManager, articles));

            var state = await ConvAccessor.GetAsync(sc.Context, () => new NewsSkillState());
            if (state.IsAction)
            {
                return await sc.EndDialogAsync(GenerateNewsActionResult(articles, true));
            }

            return await sc.EndDialogAsync();
        }
    }
}
