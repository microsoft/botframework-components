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
        private string _newsKey;

        public FindArticlesDialog(
            IServiceProvider serviceProvider)
            : base(nameof(FindArticlesDialog), serviceProvider)
        {
            _newsKey = Settings.BingNewsKey ?? throw new Exception("The BingNewsKey must be provided to use this dialog. Please provide this key in your Skill Configuration.");

            var findArticles = new WaterfallStep[]
            {
                GetMarketAsync,
                SetMarketAsync,
                GetQueryAsync,
                GetSiteAsync,
                ShowArticlesAsync,
            };

            AddDialog(new WaterfallDialog(nameof(FindArticlesDialog), findArticles));
            AddDialog(new TextPrompt(nameof(TextPrompt), MarketPromptValidatorAsync));
        }

        private async Task<DialogTurnResult> GetQueryAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var convState = await ConvAccessor.GetAsync(sc.Context, () => new NewsSkillState(), cancellationToken: cancellationToken);

            // Let's see if we have a topic
            if (convState.LuisResult != null && convState.LuisResult.Entities.topic != null && convState.LuisResult.Entities.topic.Length > 0)
            {
                return await sc.NextAsync(convState.LuisResult.Entities.topic[0]);
            }

            return await sc.PromptAsync(nameof(TextPrompt), new PromptOptions()
            {
                Prompt = TemplateManager.GenerateActivityForLocale(FindArticlesResponses.TopicPrompt)
            });
        }

        private async Task<DialogTurnResult> GetSiteAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var convState = await ConvAccessor.GetAsync(sc.Context, () => new NewsSkillState(), cancellationToken: cancellationToken);

            string query = (string)sc.Result;

            if (convState.LuisResult != null && convState.LuisResult.Entities.site != null && convState.LuisResult.Entities.site.Length > 0)
            {
                string site = convState.LuisResult.Entities.site[0].Replace(" ", string.Empty);
                query = string.Concat(query, $" site:{site}");
            }

            return await sc.NextAsync(query, cancellationToken);
        }

        private async Task<DialogTurnResult> ShowArticlesAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserAccessor.GetAsync(sc.Context, () => new NewsSkillUserState(), cancellationToken: cancellationToken);

            var query = (string)sc.Result;

            using (var client = new NewsClient(Settings.BingNewsEndPoint, _newsKey))
            {
                var articles = await client.GetNewsForTopicAsync(query, userState.Market);
                await sc.Context.SendActivityAsync(HeroCardResponses.ShowFindArticleCards(sc.Context, TemplateManager, articles), cancellationToken);

                var state = await ConvAccessor.GetAsync(sc.Context, () => new NewsSkillState(), cancellationToken: cancellationToken);
                if (state.IsAction)
                {
                    return await sc.EndDialogAsync(GenerateNewsActionResult(articles, true), cancellationToken);
                }

                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }
    }
}