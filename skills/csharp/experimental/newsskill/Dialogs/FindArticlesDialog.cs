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
using NewsSkill.Responses.FindArticles;
using NewsSkill.Services;

namespace NewsSkill.Dialogs
{
    public class FindArticlesDialog : NewsDialogBase
    {
        private NewsClient _client;

        public FindArticlesDialog(
            BotSettings settings,
            BotServices services,
            ConversationState conversationState,
            UserState userState,
            AzureMapsService mapsService,
            LocaleTemplateManager templateManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(FindArticlesDialog), settings, services, conversationState, userState, mapsService, templateManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            var newsKey = settings.BingNewsKey ?? throw new Exception("The BingNewsKey must be provided to use this dialog. Please provide this key in your Skill Configuration.");

            _client = new NewsClient(newsKey);

            var findArticles = new WaterfallStep[]
            {
                GetMarket,
                SetMarket,
                GetQuery,
                GetSite,
                ShowArticles,
            };

            AddDialog(new WaterfallDialog(nameof(FindArticlesDialog), findArticles));
            AddDialog(new TextPrompt(nameof(TextPrompt), MarketPromptValidatorAsync));
        }

        private async Task<DialogTurnResult> GetQuery(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var convState = await ConvAccessor.GetAsync(sc.Context, () => new NewsSkillState());

            // Let's see if we have a topic
            if (convState.LuisResult != null && convState.LuisResult.Entities.topic != null && convState.LuisResult.Entities.topic.Length > 0)
            {
                return await sc.NextAsync(convState.LuisResult.Entities.topic[0]);
            }

            return await sc.PromptAsync(nameof(TextPrompt), new PromptOptions()
            {
                Prompt = templateManager.GenerateActivityForLocale(FindArticlesResponses.TopicPrompt)
            });
        }

        private async Task<DialogTurnResult> GetSite(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var convState = await ConvAccessor.GetAsync(sc.Context, () => new NewsSkillState());

            string query = (string)sc.Result;

            if (convState.LuisResult != null && convState.LuisResult.Entities.site != null && convState.LuisResult.Entities.site.Length > 0)
            {
                string site = convState.LuisResult.Entities.site[0].Replace(" ", string.Empty);
                query = string.Concat(query, $" site:{site}");
            }

            return await sc.NextAsync(query);
        }

        private async Task<DialogTurnResult> ShowArticles(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserAccessor.GetAsync(sc.Context, () => new NewsSkillUserState());

            var query = (string)sc.Result;

            var articles = await _client.GetNewsForTopic(query, userState.Market);
            await sc.Context.SendActivityAsync(HeroCardResponses.ShowFindArticleCards(sc.Context, templateManager, articles));

            var skillOptions = sc.Options as NewsSkillOptionBase;
            if (skillOptions != null && skillOptions.IsAction)
            {
                return await sc.EndDialogAsync(GenerateNewsActionResult(articles, true));
            }

            return await sc.EndDialogAsync();
        }
    }
}