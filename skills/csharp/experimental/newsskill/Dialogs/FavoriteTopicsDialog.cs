// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Solutions.Responses;
using NewsSkill.Models;
using NewsSkill.Responses;
using NewsSkill.Responses.FavoriteTopics;
using NewsSkill.Services;

namespace NewsSkill.Dialogs
{
    public class FavoriteTopicsDialog : NewsDialogBase
    {
        private string _newsKey;

        public FavoriteTopicsDialog(
            IServiceProvider serviceProvider)
            : base(nameof(FavoriteTopicsDialog), serviceProvider)
        {
            _newsKey = Settings.BingNewsKey ?? throw new Exception("The BingNewsKey must be provided to use this dialog. Please provide this key in your Skill Configuration.");

            var favoriteTopics = new WaterfallStep[]
            {
                GetMarketAsync,
                SetMarketAsync,
                SetFavoritesAsync,
                ShowArticlesAsync,
            };

            AddDialog(new WaterfallDialog(nameof(FavoriteTopicsDialog), favoriteTopics));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new TextPrompt(nameof(TextPrompt), MarketPromptValidatorAsync));
        }

        private async Task<DialogTurnResult> SetFavoritesAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var convState = await ConvAccessor.GetAsync(sc.Context, () => new NewsSkillState(), cancellationToken: cancellationToken);
            var userState = await UserAccessor.GetAsync(sc.Context, () => new NewsSkillUserState(), cancellationToken: cancellationToken);

            // if intent is SetFavorites or not set in state yet
            if ((convState.LuisResult != null && convState.LuisResult.TopIntent().intent == Luis.NewsLuis.Intent.SetFavoriteTopics) || userState.Category == null)
            {
                // show card with categories the user can choose
                var categories = new PromptOptions()
                {
                    Choices = new List<Choice>(),
                };

                categories.Choices.Add(new Choice("Business"));
                categories.Choices.Add(new Choice("Entertainment"));
                categories.Choices.Add(new Choice("Health"));
                categories.Choices.Add(new Choice("Politics"));
                categories.Choices.Add(new Choice("World"));
                categories.Choices.Add(new Choice("Sports"));

                return await sc.PromptAsync(nameof(ChoicePrompt), new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivityForLocale(FavoriteTopicsResponses.FavoriteTopicPrompt),
                    Choices = categories.Choices
                },
                cancellationToken);
            }

            return await sc.NextAsync(userState.Category, cancellationToken);
        }

        private async Task<DialogTurnResult> ShowArticlesAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserAccessor.GetAsync(sc.Context, () => new NewsSkillUserState(), cancellationToken: cancellationToken);

            userState.Category = (FoundChoice)sc.Result;

            // show favorite articles
            using (var client = new NewsClient(Settings.BingNewsEndPoint, _newsKey))
            {
                var articles = await client.GetNewsByCategoryAsync(userState.Category.Value, userState.Market);
                await sc.Context.SendActivityAsync(HeroCardResponses.ShowFindArticleCards(sc.Context, TemplateManager, articles, true));

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
