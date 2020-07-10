using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdaptiveCards;
using ITSMSkill.Dialogs.Teams.Resources.Subscription;
using ITSMSkill.Extensions.Teams;
using ITSMSkill.Extensions.Teams.TaskModule;
using ITSMSkill.Proactive.Subscription;

namespace ITSMSkill.Dialogs.Teams.SubscriptionTaskModule
{
    public class SubscriptionTaskModuleAdaptiveCard
    {
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
                        Id = "UrgencyFilter",
                        Style = AdaptiveChoiceInputStyle.Compact,
                        IsMultiSelect = true,
                        Value = "UrgencyFilter",
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

        /// <returns>Adaptive Card.</returns>
        public static AdaptiveCard ThankYouForSubscribing(string response)
        {
            var card = new AdaptiveCard("1.0");
            card.Id = "ThankYouCard";

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
