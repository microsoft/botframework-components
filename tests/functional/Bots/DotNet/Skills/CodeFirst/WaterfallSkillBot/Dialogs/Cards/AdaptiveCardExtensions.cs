﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using AdaptiveCards;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Cards
{
    public static class AdaptiveCardExtensions
    {
        /// <summary>
        /// Creates a new attachment from AdaptiveCard.
        /// </summary>
        /// <param name="card"> The instance of AdaptiveCard.</param>
        /// <returns> The generated attachment.</returns>
        public static Attachment ToAttachment(this AdaptiveCard card)
        {
            return new Attachment
            {
                Content = card,
                ContentType = AdaptiveCard.ContentType,
            };
        }

        /// <summary>
        /// Wrap BotBuilder action into AdaptiveCard submit action.
        /// </summary>
        /// <param name="action"> The instance of adaptive card submit action.</param>
        /// <param name="targetAction"> Target action to be adapted.</param>
        public static void RepresentAsBotBuilderAction(this AdaptiveSubmitAction action, CardAction targetAction)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            if (targetAction == null)
            {
                throw new ArgumentNullException(nameof(targetAction));
            }

            var wrappedAction = new CardAction
            {
                Type = targetAction.Type,
                Value = targetAction.Value,
                Text = targetAction.Text,
                DisplayText = targetAction.DisplayText,
            };

            var serializerSettings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };

            var jsonStr = action.DataJson ?? "{}";
            JToken dataJson = JObject.Parse(jsonStr);
            dataJson["msteams"] = JObject.FromObject(wrappedAction, JsonSerializer.Create(serializerSettings));

            action.Title = targetAction.Title;
            action.DataJson = dataJson.ToString();
        }

        /// <summary>
        /// Wrap BotBuilder action into AdaptiveCard submit action.
        /// </summary>
        /// <param name="action"> Target bot builder action to be adapted.</param>
        /// <returns> The wrapped adaptive card submit action.</returns>
        public static AdaptiveSubmitAction ToAdaptiveCardAction(this CardAction action)
        {
            var adaptiveCardAction = new AdaptiveSubmitAction();
            adaptiveCardAction.RepresentAsBotBuilderAction(action);
            return adaptiveCardAction;
        }
    }
}
