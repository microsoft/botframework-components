// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using AdaptiveCards;
using ITSMSkill.Extensions.Teams;
using ITSMSkill.Extensions.Teams.TaskModule;
using ITSMSkill.Models;

namespace ITSMSkill.Dialogs.Teams.CreateTicketTaskModuleView
{
    /// <summary>
    /// class housing all adaptive cards related to ServiceNow Incidents.
    /// </summary>
    public class ServiceNowIncidentTaskModuleAdaptiveCardHelper
    {
        public static AdaptiveCard BuildIncidentCard(Ticket ticketResponse, string botId)
        {
            var card = new AdaptiveCard("1.0")
            {
                Id = "BuildIncidentCard",
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
                                                Text = $"Title: {ticketResponse.Title}",
                                                Wrap = true,
                                                Spacing = AdaptiveSpacing.Small,
                                                Weight = AdaptiveTextWeight.Bolder
                                            },
                                            new AdaptiveTextBlock
                                            {
                                                Text = $"Urgency: {ticketResponse.Urgency}",
                                                Color = AdaptiveTextColor.Good,
                                                MaxLines = 1,
                                                Weight = AdaptiveTextWeight.Bolder,
                                                Size = AdaptiveTextSize.Large
                                            },
                                            new AdaptiveTextBlock
                                            {
                                                Text = $"Description: {ticketResponse.Description}",
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
                        SkillId = botId,
                        TaskModuleFlowType = TeamsFlowType.UpdateTicket_Form.ToString(),
                        FlowData = new Dictionary<string, object>
                        {
                            { "IncidentDetails", ticketResponse }
                        },
                    }
                }
            });

            card.Actions.Add(new AdaptiveSubmitAction()
            {
                Title = "Delete Incident",
                Data = new AdaptiveCardValue<TaskModuleMetadata>()
                {
                    Data = new TaskModuleMetadata()
                    {
                        SkillId = botId,
                        TaskModuleFlowType = TeamsFlowType.DeleteTicket_Form.ToString(),
                        FlowData = new Dictionary<string, object>
                        {
                            { "IncidentId", ticketResponse.Id }
                        },
                        Submit = true
                    }
                }
            });

            return card;
        }

        public static AdaptiveCard CloseIncidentCard(Ticket ticketResponse)
        {
            var card = new AdaptiveCard("1.0")
            {
                Id = "CloseIncidentResponseCard",
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
                                                Text = $"Incident With TicketId: {ticketResponse.Id} is closed with Reason:  {ticketResponse.ResolvedReason}",
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

            return card;
        }

        /// <returns>Adaptive Card.</returns>
        public static AdaptiveCard IncidentResponseCard(string trackerResponse)
        {
            var card = new AdaptiveCard("1.0");
            card.Id = "IncidentResponseCard";

            var columns = new List<AdaptiveColumn>
            {
                new AdaptiveColumn
                {
                    VerticalContentAlignment = AdaptiveVerticalContentAlignment.Center,
                    Items = new List<AdaptiveElement>
                        {
                            new AdaptiveTextBlock
                            {
                                Text = trackerResponse,
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
