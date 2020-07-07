// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AdaptiveCards;
using GenericITSMSkill.Teams.TaskModule;

namespace GenericITSMSkill.Dialogs.Helper.AdaptiveCardHelper
{
    public class GenericITSMSkillAdaptiveCards
    {
        public static AdaptiveCard GetSubscriptionAdaptiveCard(string botId)
        {
            int sevInd = 0;
            var card = new AdaptiveCard("1.0")
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        Text = GenericITSMStrings.FlowName,
                        Size = AdaptiveTextSize.Medium,
                        IsSubtle = true,
                        Weight = AdaptiveTextWeight.Bolder
                    },
                    new AdaptiveTextInput
                    {
                        Id = "FlowName",
                        Placeholder = GenericITSMStrings.FlowName,
                        Spacing = AdaptiveSpacing.Small,
                        IsMultiline = true
                    },
                    new AdaptiveTextBlock
                    {
                        Text = GenericITSMStrings.ServiceName,
                        Size = AdaptiveTextSize.Medium,
                        IsSubtle = true,
                        Weight = AdaptiveTextWeight.Bolder
                    },
                    new AdaptiveChoiceSetInput
                    {
                        Id = "ServiceChoices",
                        Style = AdaptiveChoiceInputStyle.Compact,
                        IsMultiSelect = true,
                        Value = "ServiceChoice",
                        Choices = GenericITSMStrings.ServiceList.Select(it => new AdaptiveChoice
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
                            Title = "Submit",
                            Data = new AdaptiveCardValue<TaskModuleMetadata>()
                            {
                                Data = new TaskModuleMetadata()
                                {
                                    SkillId = botId,
                                    TaskModuleFlowType = TeamsFlowType.CreateFlow.ToString(),
                                    Submit = true
                                }
                            }
                        }
                    }
            };

            return card;
        }

        public static AdaptiveCard CreateFlowUrlAdaptiveCard(string botId)
        {
            try
            {
                // Json Card for creating incident
                // TODO: Replace with Cards.Lg and responses
                var card = new AdaptiveCard("1.0")
                {
                    Body = new List<AdaptiveElement>
                    {
                        new AdaptiveTextBlock
                        {
                            Text = "Press Button below to invoke a task module to create FlowUrl",
                            Size = AdaptiveTextSize.Medium,
                            IsSubtle = true,
                            Weight = AdaptiveTextWeight.Bolder
                        },
                    }
                };
                card.Id = "GetUserInput";
                card.Actions.Add(new AdaptiveSubmitAction()
                {
                    Title = "Create FlowURL",
                    Data = new AdaptiveCardValue<TaskModuleMetadata>()
                    {
                        Data = new TaskModuleMetadata()
                        {
                            SkillId = botId,
                            TaskModuleFlowType = TeamsFlowType.CreateFlow.ToString(),
                        }
                    }
                });

                return card;
            }
            catch (Exception)
            {
                // handle JSON parsing error
                // or, re-throw
                throw;
            }
        }

        public static AdaptiveCard FlowUrlResponseCard(string response)
        {
            var card = new AdaptiveCard("1.0")
            {
                Body = new List<AdaptiveElement>
                {
                    new AdaptiveTextBlock
                    {
                        Text = GenericITSMStrings.FlowUrlResponse,
                        Size = AdaptiveTextSize.Medium,
                        IsSubtle = true,
                        Weight = AdaptiveTextWeight.Bolder
                    },
                    new AdaptiveTextBlock
                    {
                        Text = response,
                        Size = AdaptiveTextSize.Medium,
                        IsSubtle = true,
                        Weight = AdaptiveTextWeight.Bolder,
                    },
                }
            };

            return card;
        }
    }
}
