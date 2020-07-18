// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ITSMSkill.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AdaptiveCards;
    using ITSMSkill.Models.ServiceNow;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Newtonsoft.Json;

    public static class NotificationToAdaptiveCardExtension
    {
        /// <summary>
        /// Creates AdaptiveCard from Notification.
        /// </summary>
        /// <param name="serviceNowNotification">ServiceNow Notification.</param>
        /// <returns>AdaptiveCard.</returns>
        public static AdaptiveCard ToAdaptiveCard(this ServiceNowNotification serviceNowNotification)
        {
            var card = new AdaptiveCard("1.0")
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveContainer
                    {
                        Items = new List<AdaptiveElement>
                        {
                            new AdaptiveColumnSet
                            {
                                Columns = new List<AdaptiveColumn>
                                {
                                    new AdaptiveColumn
                                    {
                                        Width = AdaptiveColumnWidth.Stretch,
                                        Items = new List<AdaptiveElement>
                                        {
                                            new AdaptiveTextBlock
                                            {
                                                Text = $"Incident Number: {serviceNowNotification.Id}",
                                                Wrap = true,
                                                Spacing = AdaptiveSpacing.Small,
                                                Weight = AdaptiveTextWeight.Bolder
                                            },
                                            new AdaptiveTextBlock
                                            {
                                                Text = $"Title: {serviceNowNotification.Title}",
                                                Wrap = true,
                                                Spacing = AdaptiveSpacing.Small,
                                                Weight = AdaptiveTextWeight.Bolder
                                            },
                                            new AdaptiveTextBlock
                                            {
                                                Text = $"Urgency: {serviceNowNotification.Urgency}",
                                                Color = serviceNowNotification.Urgency.Equals("1") ? AdaptiveTextColor.Attention : serviceNowNotification.Urgency.Equals("2") ? AdaptiveTextColor.Warning : AdaptiveTextColor.Good,
                                                MaxLines = 1,
                                                Weight = AdaptiveTextWeight.Bolder,
                                                Size = AdaptiveTextSize.Large
                                            },
                                            new AdaptiveTextBlock
                                            {
                                                Text = $"Description: {serviceNowNotification.Description}",
                                                Wrap = true,
                                                Spacing = AdaptiveSpacing.Small,
                                                Weight = AdaptiveTextWeight.Bolder
                                            },
                                            new AdaptiveTextBlock
                                            {
                                                Text = $"Impact: {serviceNowNotification.Impact}",
                                                Wrap = true,
                                                Spacing = AdaptiveSpacing.Small,
                                                Weight = AdaptiveTextWeight.Bolder
                                            },
                                            new AdaptiveTextBlock
                                            {
                                                Text = $"Updated By: {serviceNowNotification.UpdatedBy}",
                                                Wrap = true,
                                                Spacing = AdaptiveSpacing.Small,
                                                Weight = AdaptiveTextWeight.Bolder
                                            },
                                            new AdaptiveTextBlock
                                            {
                                                Text = "Updated Time: " + string.Format("{0:HH:mm:ss tt}", DateTime.Now),
                                                Wrap = true,
                                                Spacing = AdaptiveSpacing.Small,
                                                Weight = AdaptiveTextWeight.Bolder
                                            },
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            return card;
        }
    }
}
