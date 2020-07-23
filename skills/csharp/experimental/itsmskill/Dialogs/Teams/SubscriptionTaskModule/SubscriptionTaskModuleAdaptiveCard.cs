// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using AdaptiveCards;
using ITSMSkill.Extensions.Teams;
using ITSMSkill.Extensions.Teams.TaskModule;
using ITSMSkill.Models.ServiceNow;
using ITSMSkill.Proactive.Subscription;

namespace ITSMSkill.Dialogs.Teams.SubscriptionTaskModule
{
    /// <summary>
    /// class housing all subscription related adaptive cards.
    /// </summary>
    public class SubscriptionTaskModuleAdaptiveCard
    {
        // Use this adaptive card to get Subscription Details from User
        public static AdaptiveCard GetSubcriptionInputCard(string botId = null)
        {
            var card = new AdaptiveCard("1.0");

            var columns = new List<AdaptiveColumn>
            {
                new AdaptiveColumn
                {
                    VerticalContentAlignment = AdaptiveVerticalContentAlignment.Center,
                    Items = new List<AdaptiveElement>
                        {
                            new AdaptiveTextBlock
                            {
                                Text = "Please Click Create Subscription To Create New Subscriptions",
                                Size = AdaptiveTextSize.Small,
                                Weight = AdaptiveTextWeight.Bolder,
                                Color = AdaptiveTextColor.Accent,
                                Wrap = true
                            }
                        },
                }
            };

            var columnSet = new AdaptiveColumnSet
            {
                Columns = columns,
                Separator = true
            };

            var list = new List<AdaptiveElement>
            {
                columnSet
            };

            card.Body.AddRange(list);
            card?.Actions.Add(new AdaptiveSubmitAction()
            {
                Title = "Create Subscription",
                Data = new AdaptiveCardValue<TaskModuleMetadata>()
                {
                    Data = new TaskModuleMetadata()
                    {
                        SkillId = botId,
                        TaskModuleFlowType = TeamsFlowType.CreateSubscription_Form.ToString(),
                    }
                }
            });

            return card;
        }

        // Use this adaptive card when we want to update existing subscription
        public static AdaptiveCard GetSubscriptionAdaptiveCard(string botId)
        {
            int filterCondition = 0;
            var card = new AdaptiveCard("1.0")
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        Text = IncidentSubscriptionStrings.FilterName,
                        Size = AdaptiveTextSize.Medium,
                        IsSubtle = true,
                        Weight = AdaptiveTextWeight.Bolder
                    },
                    new AdaptiveTextInput
                    {
                        Id = "FilterName",
                        Placeholder = IncidentSubscriptionStrings.FilterName,
                        Spacing = AdaptiveSpacing.Small,
                        IsMultiline = true
                    },
                    new AdaptiveTextBlock
                    {
                        Text = IncidentSubscriptionStrings.NotificationNameSpace,
                        Size = AdaptiveTextSize.Medium,
                        IsSubtle = true,
                        Weight = AdaptiveTextWeight.Bolder
                    },
                    new AdaptiveTextInput
                    {
                        Id = "NotificationNameSpace",
                        Placeholder = IncidentSubscriptionStrings.PostNotificationAPIName,
                        Spacing = AdaptiveSpacing.Small,
                        IsMultiline = true
                    },
                    new AdaptiveTextBlock
                    {
                        Text = IncidentSubscriptionStrings.PostNotificationAPIName,
                        Size = AdaptiveTextSize.Medium,
                        IsSubtle = true,
                        Weight = AdaptiveTextWeight.Bolder
                    },
                    new AdaptiveTextInput
                    {
                        Id = "PostNotificationAPIName",
                        Placeholder = IncidentSubscriptionStrings.PostNotificationAPIName,
                        Spacing = AdaptiveSpacing.Small,
                        IsMultiline = true
                    },
                    new AdaptiveTextBlock
                    {
                        Text = IncidentSubscriptionStrings.SeverityTextBlock,
                        Size = AdaptiveTextSize.Medium,
                        IsSubtle = true,
                        Weight = AdaptiveTextWeight.Bolder
                    },
                    new AdaptiveChoiceSetInput
                    {
                        Id = "FilterCondition",
                        Style = AdaptiveChoiceInputStyle.Compact,
                        IsMultiSelect = true,
                        Value = "FilterCondition",
                        Choices = IncidentSubscriptionStrings.FilterConditions.Select(it => new AdaptiveChoice
                        {
                            Title = $"{it}",
                            Value = filterCondition++.ToString()
                        }).ToList()
                    }
                },
                Actions = new List<AdaptiveAction>
                    {
                        new AdaptiveSubmitAction
                        {
                            Title = "Submit",
                            Data = new AdaptiveCardValue<TaskModuleMetadata>()
                            {
                                Data = new TaskModuleMetadata()
                                {
                                    SkillId = botId,
                                    TaskModuleFlowType = TeamsFlowType.CreateSubscription_Form.ToString(),
                                    Submit = true
                                }
                            }
                        }
                    }
            };

            return card;
        }

        // Use this adaptive card when we have Subscription Details from User
        public static AdaptiveCard BuildSubscriptionAdaptiveCard(ServiceNowSubscription serviceNowSubscription, string botId)
        {
            int filterCondition = 0;

            var card = new AdaptiveCard("1.0")
            {
                Id = "BuildSubscriptionCard",
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
                                                Text = $"FilterName: {serviceNowSubscription.FilterName}",
                                                Wrap = true,
                                                Spacing = AdaptiveSpacing.Small,
                                                Weight = AdaptiveTextWeight.Bolder
                                            },
                                            new AdaptiveTextBlock
                                            {
                                                Text = $"NotificationNameSpace: {serviceNowSubscription.NotificationNameSpace}",
                                                Color = AdaptiveTextColor.Good,
                                                MaxLines = 1,
                                                Weight = AdaptiveTextWeight.Bolder,
                                                Size = AdaptiveTextSize.Large
                                            },
                                            new AdaptiveTextBlock
                                            {
                                                Text = $"NotificationApiName: {serviceNowSubscription.NotificationApiName}",
                                                Color = AdaptiveTextColor.Good,
                                                MaxLines = 1,
                                                Weight = AdaptiveTextWeight.Bolder,
                                                Size = AdaptiveTextSize.Large
                                            },
                                            new AdaptiveChoiceSetInput
                                            {
                                                Id = "FilterCondition",
                                                Style = AdaptiveChoiceInputStyle.Compact,
                                                IsMultiSelect = true,
                                                Value = serviceNowSubscription.FilterCondition,
                                                Choices = IncidentSubscriptionStrings.FilterConditions.Select(it => new AdaptiveChoice
                                                {
                                                    Title = $"{it}",
                                                    Value = filterCondition++.ToString()
                                                }).ToList()
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            card.Actions.Add(new AdaptiveSubmitAction()
            {
                Title = "Update Subscription",
                Data = new AdaptiveCardValue<TaskModuleMetadata>()
                {
                    Data = new TaskModuleMetadata()
                    {
                        SkillId = botId,
                        TaskModuleFlowType = TeamsFlowType.UpdateTicket_Form.ToString(),
                        FlowData = new Dictionary<string, object>
                        {
                            { "SubscriptionDetails", serviceNowSubscription }
                        },
                    }
                }
            });

            card.Actions.Add(new AdaptiveSubmitAction()
            {
                Title = "Delete Subscription",
                Data = new AdaptiveCardValue<TaskModuleMetadata>()
                {
                    Data = new TaskModuleMetadata()
                    {
                        SkillId = botId,
                        TaskModuleFlowType = TeamsFlowType.DeleteTicket_Form.ToString(),
                        FlowData = new Dictionary<string, object>
                        {
                            { "SubscriptionDetails", serviceNowSubscription }
                        },
                        Submit = true
                    }
                }
            });

            return card;
        }

        // Use this adaptive card when creating a adaptive card with Existing SubscriptionDetails
        public static AdaptiveCard PresentSubscriptionAdaptiveCard(ServiceNowSubscription serviceNowSubscription, string botId)
        {
            int filterCondition = 0;
            var card = new AdaptiveCard("1.0")
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        Text = IncidentSubscriptionStrings.FilterName + $": {serviceNowSubscription.FilterName}",
                        Size = AdaptiveTextSize.Medium,
                        IsSubtle = true,
                        Weight = AdaptiveTextWeight.Bolder
                    },
                    new AdaptiveTextBlock
                    {
                        Text = IncidentSubscriptionStrings.NotificationNameSpace + $": {serviceNowSubscription.NotificationNameSpace}",
                        Size = AdaptiveTextSize.Medium,
                        IsSubtle = true,
                        Weight = AdaptiveTextWeight.Bolder
                    },
                    new AdaptiveTextBlock
                    {
                        Text = IncidentSubscriptionStrings.PostNotificationAPIName + $": {serviceNowSubscription.NotificationApiName}",
                        Size = AdaptiveTextSize.Medium,
                        IsSubtle = true,
                        Weight = AdaptiveTextWeight.Bolder
                    },
                    new AdaptiveChoiceSetInput
                    {
                        Id = "FilterCondition",
                        Style = AdaptiveChoiceInputStyle.Compact,
                        IsMultiSelect = true,
                        Value = serviceNowSubscription.FilterCondition,
                        Choices = IncidentSubscriptionStrings.FilterConditions.Select(it => new AdaptiveChoice
                        {
                            Title = $"{it}",
                            Value = filterCondition++.ToString()
                        }).ToList()
                    }
                },
                Actions = new List<AdaptiveAction>
                {
                    new AdaptiveSubmitAction
                    {
                        Title = "Submit",
                        Data = new AdaptiveCardValue<TaskModuleMetadata>()
                        {
                            Data = new TaskModuleMetadata()
                            {
                                SkillId = botId,
                                TaskModuleFlowType = TeamsFlowType.UpdateTicket_Form.ToString(),
                                Submit = true,
                            }
                        }
                    }
                }
            };

            return card;
        }

        /// <returns>Adaptive Card.</returns>
        public static AdaptiveCard SubscriptionResponseCard(string response)
        {
            var card = new AdaptiveCard("1.0");
            card.Id = "ResponseCard";

            var columns = new List<AdaptiveColumn>
            {
                new AdaptiveColumn
                {
                    VerticalContentAlignment = AdaptiveVerticalContentAlignment.Center,
                    Items = new List<AdaptiveElement>
                        {
                            new AdaptiveTextBlock
                            {
                                Text = response,
                                Size = AdaptiveTextSize.Small,
                                Weight = AdaptiveTextWeight.Bolder,
                                Color = AdaptiveTextColor.Accent,
                                Wrap = true
                            }
                        }
                }
            };

            return card;
        }
    }
}
