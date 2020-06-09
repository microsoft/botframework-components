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

        public static AdaptiveCard GetSubscriptionAdaptiveCard()
        {
            int sevInd = 0;
            var card = new AdaptiveCard("1.0")
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        Text = IncidentSubscriptionStrings.SeverityTextBlock,
                        Size = AdaptiveTextSize.Medium,
                        IsSubtle = true,
                        Weight = AdaptiveTextWeight.Bolder
                    },
                    new AdaptiveChoiceSetInput
                    {
                        Id = "SeverityFilter",
                        Style = AdaptiveChoiceInputStyle.Compact,
                        IsMultiSelect = true,
                        Value = "UrgencyFilter",
                        Choices = IncidentSubscriptionStrings.Severities.Select(it => new AdaptiveChoice
                        {
                            Title = $"{it}",
                            Value = sevInd++.ToString()
                        }).ToList()
                    }
                },
                Actions = new List<AdaptiveAction>
                    {
                        new AdaptiveSubmitAction
                        {
                            Title = "Submit"
                        },
                        new AdaptiveSubmitAction
                        {
                            Title = "Cancel",
                            Data = new SubscriptionFormInputData
                            {
                                Action = SubscriptionFormInputData.Action_Cancel
                            }
                        }
                    }
            };

            return card;
        }
    }
}
