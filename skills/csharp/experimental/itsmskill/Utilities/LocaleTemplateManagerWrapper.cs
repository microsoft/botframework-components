// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Newtonsoft.Json.Linq;

namespace ITSMSkill.Utilities
{
    public static class LocaleTemplateManagerWrapper
    {
        // TODO may not all be same
        public static readonly string PathBase = @"..\..\Content";

        private const string CardsOnly = "CardsOnly";

        public static Activity GenerateActivity(this LocaleTemplateManager manager, Card card)
        {
            return manager.GenerateActivity(new Card[] { card });
        }

        public static Activity GenerateActivity(this LocaleTemplateManager manager, IEnumerable<Card> cards, string attachmentLayout = "carousel")
        {
            return manager.GenerateActivity(CardsOnly, cards, null, attachmentLayout);
        }

        public static Activity GenerateActivity(this LocaleTemplateManager manager, string templateId, Card card, IDictionary<string, object> tokens = null)
        {
            return manager.GenerateActivity(templateId, new Card[] { card }, tokens);
        }

        public static Activity GenerateActivity(this LocaleTemplateManager manager, string templateId, IEnumerable<Card> cards, IDictionary<string, object> tokens = null, string attachmentLayout = "carousel")
        {
            if (string.IsNullOrEmpty(templateId))
            {
                templateId = CardsOnly;
            }

            var input = new
            {
                Data = tokens,
                Cards = cards.Select((card) => { return Convert(card); }).ToArray(),
                Layout = attachmentLayout,
            };
            try
            {
                return manager.GenerateActivityForLocale(templateId, input);
            }
            catch (Exception ex)
            {
                var result = Activity.CreateMessageActivity();
                result.Text = ex.Message;
                return (Activity)result;
            }
        }

        public static Activity GenerateActivity(this LocaleTemplateManager manager, string templateId, Card card, IDictionary<string, object> tokens = null, string containerName = null, IEnumerable<Card> containerItems = null)
        {
            throw new Exception("1. create *Containee.json which only keeps containee's body;2. in the container, write ${if(Cards==null,'',join(foreach(Cards,Card,CreateStringNoContainer(Card.Name,Card.Data)),','))}");

            if (string.IsNullOrEmpty(templateId))
            {
                templateId = CardsOnly;
            }

            var input = new
            {
                Data = tokens,
                Cards = new CardExt[] { Convert(card, containerItems: containerItems) },
            };
            try
            {
                return manager.GenerateActivityForLocale(templateId, input);
            }
            catch (Exception ex)
            {
                var result = Activity.CreateMessageActivity();
                result.Text = ex.Message;
                return (Activity)result;
            }
        }

        public static Activity GenerateActivity(this LocaleTemplateManager manager, string templateId, IDictionary<string, object> tokens = null)
        {
            return manager.GenerateActivity(templateId, Array.Empty<Card>(), tokens);
        }

        public static string GetString(this LocaleTemplateManager manager, string templateId)
        {
            // Not use .Text in case text and speak are different
            return manager.GenerateActivityForLocale(templateId).Text;
        }

        public static string[] ParseReplies(this Templates manager, string name, IDictionary<string, object> data = null)
        {
            var input = new
            {
                Data = data
            };

            return manager.ExpandTemplate(name + ".Text", input).Select(obj => obj.ToString()).ToArray();
        }

        public static Templates CreateTemplates()
        {
            return Templates.ParseFile(Path.Join(@"Responses\ResponsesAndTexts", $"ResponsesAndTexts.lg"));
        }

        public static CardExt Convert(Card card, string suffix = ".json", IEnumerable<Card> containerItems = null)
        {
            var res = new CardExt { Name = Path.Join(PathBase, card.Name + suffix), Data = card.Data };
            if (containerItems != null)
            {
                res.Cards = containerItems.Select((card) => Convert(card, "Containee.json")).ToList();
            }

            return res;
        }

        // first locale is default locale
        public static LocaleTemplateManager CreateLocaleTemplateManager(params string[] locales)
        {
            var localizedTemplates = new Dictionary<string, string>();
            foreach (var locale in locales)
            {
                string localeTemplateFile = null;

                // LG template for default locale should not include locale in file extension.
                if (locale.Equals(locales[0]))
                {
                    localeTemplateFile = Path.Join(@"Responses\ResponsesAndTexts", $"ResponsesAndTexts.lg");
                }
                else
                {
                    localeTemplateFile = Path.Join(@"Responses\ResponsesAndTexts", $"ResponsesAndTexts.{locale}.lg");
                }

                localizedTemplates.Add(locale, localeTemplateFile);
            }

            return new LocaleTemplateManager(localizedTemplates, locales[0]);
        }

        public class CardExt : Card
        {
            public List<CardExt> Cards { get; set; }
        }
    }
}
