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
        private string _newsKey;

        public TrendingArticlesDialog(
            IServiceProvider serviceProvider)
            : base(nameof(TrendingArticlesDialog), serviceProvider)
        {
            _newsKey = Settings.BingNewsKey ?? throw new Exception("The BingNewsKey must be provided to use this dialog. Please provide this key in your Skill Configuration.");

            var trendingArticles = new WaterfallStep[]
            {
                GetMarketAsync,
                SetMarketAsync,
                ShowArticlesAsync,
            };

            AddDialog(new WaterfallDialog(nameof(TrendingArticlesDialog), trendingArticles));
            AddDialog(new TextPrompt(nameof(TextPrompt), MarketPromptValidatorAsync));
        }

        private async Task<DialogTurnResult> ShowArticlesAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserAccessor.GetAsync(sc.Context, () => new NewsSkillUserState(), cancellationToken: cancellationToken);

            using (var client = new NewsClient(Settings.BingNewsEndPoint, _newsKey))
            {
                var articles = await client.GetTrendingNewsAsync(userState.Market);
                await sc.Context.SendActivityAsync(HeroCardResponses.ShowTrendingCards(sc.Context, TemplateManager, articles), cancellationToken);

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
