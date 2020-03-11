using Microsoft.Azure.CognitiveServices.Search.NewsSearch.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using NewsSkill.Responses.FavoriteTopics;
using NewsSkill.Responses.FindArticles;
using NewsSkill.Responses.Main;
using NewsSkill.Responses.TrendingArticles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewsSkill.Responses
{
    public class HeroCardResponses
    {
        public static IMessageActivity SendIntroCard(ITurnContext turnContext, LocaleTemplateEngineManager localeTemplateEngineManager)
        {
            var response = turnContext.Activity.CreateReply();

            response.Attachments = new List<Attachment>()
            {
                new HeroCard()
                {
                    Title = localeTemplateEngineManager.GenerateActivityForLocale(MainStrings.INTRO_TITLE).Text,
                    Text = localeTemplateEngineManager.GenerateActivityForLocale(MainStrings.INTRO_TEXT).Text
                }.ToAttachment()
            };

            return response;
        }

        public static IMessageActivity SendHelpCard(ITurnContext turnContext, LocaleTemplateEngineManager localeTemplateEngineManager)
        {
            var response = turnContext.Activity.CreateReply();
            response.Attachments = new List<Attachment>
            {
                new HeroCard()
                {
                    Title = localeTemplateEngineManager.GenerateActivityForLocale(MainStrings.HELP_TITLE).Text,
                    Text = localeTemplateEngineManager.GenerateActivityForLocale(MainStrings.HELP_TEXT).Text,
                    Buttons = new List<CardAction>()
                {
                    new CardAction(type: ActionTypes.ImBack, title: "Test", value: "Hello"),
                    new CardAction(type: ActionTypes.OpenUrl, title: "Learn More", value: "https://docs.microsoft.com/en-us/azure/bot-service/?view=azure-bot-service-4.0"),
                },
                }.ToAttachment()
            };

            return response;
        }

        public static IMessageActivity ShowFindArticleCards(ITurnContext context, LocaleTemplateEngineManager localeTemplateEngineManager, dynamic data, bool isFindingFavorite = false)
        {
            var response = context.Activity.CreateReply();
            var articles = data as List<NewsArticle>;

            if (articles.Any())
            {
                Activity articleResponse = null;
                if (isFindingFavorite)
                {
                    articleResponse = localeTemplateEngineManager.GenerateActivityForLocale(FavoriteTopicsResponses.ShowFavoriteTopics, new { Count = articles.Count, Article = articles[0] });
                }
                else
                {
                    articleResponse = localeTemplateEngineManager.GenerateActivityForLocale(FindArticlesResponses.ShowArticles, new { Count = articles.Count, Article = articles[0] });
                }

                response.Text = articleResponse.Text;
                response.Speak = articleResponse.Speak;

                response.Attachments = new List<Attachment>();
                response.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                foreach (var item in articles)
                {
                    var card = new ThumbnailCard()
                    {
                        Title = item.Name,
                        Subtitle = item.DatePublished,
                        Text = item.Description,
                        Images = item?.Image?.Thumbnail?.ContentUrl != null ? new List<CardImage>()
                        {
                            new CardImage(item.Image.Thumbnail.ContentUrl),
                        }
                        : null,
                        Buttons = new List<CardAction>()
                        {
                            new CardAction(ActionTypes.OpenUrl, title: localeTemplateEngineManager.GenerateActivityForLocale(MainStrings.ReadMore).Text, value: item.Url)
                        },
                    }.ToAttachment();

                    response.Attachments.Add(card);
                }
            }
            else
            {
                if (isFindingFavorite)
                {
                    response = localeTemplateEngineManager.GenerateActivityForLocale(FavoriteTopicsResponses.NoFavoriteTopics);
                }
                else
                {
                    response = localeTemplateEngineManager.GenerateActivityForLocale(FindArticlesResponses.NoArticles);
                }
            }

            return response;
        }

        public static Activity ShowTrendingCards(ITurnContext context, LocaleTemplateEngineManager localeTemplateEngineManager, dynamic data)
        {
            var response = context.Activity.CreateReply();
            var articles = data as List<NewsTopic>;

            if (articles.Any())
            {
                var trendingResponse = localeTemplateEngineManager.GenerateActivityForLocale(FavoriteTopicsResponses.ShowFavoriteTopics, new { Count = articles.Count, Article = articles[0] });
                response.Text = trendingResponse.Text;
                response.Speak = trendingResponse.Speak;

                response.Attachments = new List<Attachment>();
                response.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                foreach (var item in articles)
                {
                    var card = new HeroCard()
                    {
                        Title = item.Name,
                        Images = item?.Image?.Url != null ? new List<CardImage>()
                        {
                            new CardImage(item.Image.Url),
                        }
                        : null,
                        Buttons = new List<CardAction>()
                        {
                            new CardAction(ActionTypes.OpenUrl, title: localeTemplateEngineManager.GenerateActivityForLocale(MainStrings.ReadMore).Text, value: item.WebSearchUrl)
                        },
                    }.ToAttachment();

                    response.Attachments.Add(card);
                }
            }
            else
            {
                response = localeTemplateEngineManager.GenerateActivityForLocale(TrendingArticlesResponses.NoTrending);
            }

            return response;
        }

        private static object ShowFindArticlesCards(ITurnContext context, dynamic data)
        {
            var response = context.Activity.CreateReply();
            var articles = data as List<NewsArticle>;

            if (articles.Any())
            {
                response.Text = "Here's what I found:";

                if (articles.Count > 1)
                {
                    response.Speak = $"I found a few news stories, here's a summary of the first: {articles[0].Description}";
                }
                else
                {
                    response.Speak = $"{articles[0].Description}";
                }

                response.Attachments = new List<Attachment>();
                response.AttachmentLayout = AttachmentLayoutTypes.Carousel;

                foreach (var item in articles)
                {
                    var card = new ThumbnailCard()
                    {
                        Title = item.Name,
                        Subtitle = item.DatePublished,
                        Text = item.Description,
                        Images = item?.Image?.Thumbnail?.ContentUrl != null ? new List<CardImage>()
                        {
                            new CardImage(item.Image.Thumbnail.ContentUrl),
                        }
                        : null,
                        Buttons = new List<CardAction>()
                        {
                            new CardAction(ActionTypes.OpenUrl, title: "Read more", value: item.Url)
                        },
                    }.ToAttachment();

                    response.Attachments.Add(card);
                }
            }
            else
            {
                response.Text = "Sorry, I couldn't find any articles on that topic.";
                response.Speak = "Sorry, I couldn't find any articles on that topic.";
            }

            return response;
        }
    }
}
