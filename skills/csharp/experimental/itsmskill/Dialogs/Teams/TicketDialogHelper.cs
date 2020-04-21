// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ITSMSkill.Dialogs.Teams
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using AdaptiveCards;
    using ITSMSkill.Extensions.Teams;
    using ITSMSkill.Extensions.Teams.TaskModule;
    using ITSMSkill.Models;
    using ITSMSkill.Utilities;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json;

    /// <summary>
    /// Helper class to create adaptive cards
    /// </summary>
    public class TicketDialogHelper
    {
        public static AdaptiveCard CreateIncidentAdaptiveCard()
        {
            try
            {
                // Json Card for creating incident
                // TODO: Replace with Cards.Lg and responses
                AdaptiveCard adaptiveCard = AdaptiveCardHelper.GetCardFromJson("Dialogs/Teams/Resources/CreateIncident.json");
                adaptiveCard.Id = "GetUserInput";
                adaptiveCard.Actions.Add(new AdaptiveSubmitAction()
                {
                    Title = "SubmitIncident",
                    Data = new AdaptiveCardValue<TaskModuleMetadata>()
                    {
                        Data = new TaskModuleMetadata()
                        {
                            TaskModuleFlowType = TeamsFlowType.CreateTicket_Form.ToString(),
                            Submit = true
                        }
                    }
                });

                return adaptiveCard;
            }
            catch (AdaptiveSerializationException e)
            {
                // handle JSON parsing error
                // or, re-throw
                throw;
            }
        }

        public static AdaptiveCard UpdateIncidentCard(Ticket details)
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
                                                Text = $"Title: {details.Title}",
                                                Wrap = true,
                                                Spacing = AdaptiveSpacing.Small,
                                                Weight = AdaptiveTextWeight.Bolder
                                            },
                                            new AdaptiveTextBlock
                                            {
                                                // Incase of IcmForwarder, Triggers do not have incidentUrl hence being explicit here
                                                Text = $"Urgency: {details.Urgency}",
                                                Color = AdaptiveTextColor.Good,
                                                MaxLines = 1,
                                                Weight = AdaptiveTextWeight.Bolder,
                                                Size = AdaptiveTextSize.Large
                                            },
                                            new AdaptiveTextBlock
                                            {
                                                Text = $"Description: {details.Description}",
                                                Wrap = true,
                                                Spacing = AdaptiveSpacing.Small,
                                                Weight = AdaptiveTextWeight.Bolder
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
                Title = "Update Incident",
                Data = new AdaptiveCardValue<TaskModuleMetadata>()
                {
                    Data = new TaskModuleMetadata()
                    {
                        TaskModuleFlowType = TeamsFlowType.UpdateTicket_Form.ToString(),
                        Submit = true
                    }
                }
            });

            return card;
        }

        // <returns> Adaptive Card.</returns>
        public static AdaptiveCard ServiceNowTickHubAdaptiveCard()
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
                                Text = "Please Click Create Ticket To Create New Incident",
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
                Title = "Create Ticket",
                Data = new AdaptiveCardValue<TaskModuleMetadata>()
                {
                    Data = new TaskModuleMetadata()
                    {
                        TaskModuleFlowType = TeamsFlowType.CreateTicket_Form.ToString()
                    }
                }
            });

            return card;
        }
    }
}
