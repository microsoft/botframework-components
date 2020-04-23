namespace ITSMSkill.Dialogs.Teams.CreateTicketTaskModuleView
{
    using System.Collections.Generic;
    using AdaptiveCards;
    using ITSMSkill.Dialogs.Teams;
    using ITSMSkill.Extensions.Teams;
    using ITSMSkill.Extensions.Teams.TaskModule;
    using ITSMSkill.Models;
    using Microsoft.Bot.Schema;

    public class RenderCreateIncidentHelper
    {
        public static TaskEnvelope GetUserInput()
        {
            var response = new TaskEnvelope
            {
                Task = new TaskProperty()
                {
                    Type = "continue",
                    TaskInfo = new TaskInfo()
                    {
                        Title = "GetUserInput",
                        Height = "medium",
                        Width = 500,
                        Card = new Attachment
                        {
                            ContentType = AdaptiveCard.ContentType,
                            Content = TicketDialogHelper.CreateIncidentAdaptiveCard()
                        }
                    }
                }
            };

            return response;
        }

        public static TaskEnvelope IncidentAddFailed()
        {
            var response = new TaskEnvelope
            {
                Task = new TaskProperty()
                {
                    Type = "continue",
                    TaskInfo = new TaskInfo()
                    {
                        Title = "Incident Create Failed",
                        Height = "small",
                        Width = 500,
                        Card = new Attachment
                        {
                            ContentType = AdaptiveCard.ContentType,
                            Content = ImpactTrackerResponseCard("Incident Create Failed")
                        }
                    }
                }
            };

            return response;
        }

        public static TaskEnvelope ImpactAddEnvelope()
        {
            var response = new TaskEnvelope
            {
                Task = new TaskProperty()
                {
                    Type = "continue",
                    TaskInfo = new TaskInfo()
                    {
                        Title = "IncidentAdded",
                        Height = "small",
                        Width = 500,
                        Card = new Attachment
                        {
                            ContentType = AdaptiveCard.ContentType,
                            Content = ImpactTrackerResponseCard("Incident has been created")
                        }
                    }
                }
            };

            return response;
        }

        public static AdaptiveCard BuildTicketCard(Ticket ticketResponse)
        {
            var card = new AdaptiveCard("1.0")
            {
                Id = "IncidentResponseCard",
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

        public static AdaptiveCard CloseTicketCard(Ticket ticketResponse)
        {
            var card = new AdaptiveCard("1.0")
            {
                Id = "IncidentResponseCard",
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
                                                Text = $"Ticket With TicketId: {ticketResponse.Id} is closed with Reason:  {ticketResponse.ResolvedReason}",
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
        public static AdaptiveCard ImpactTrackerResponseCard(string trackerResponse)
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
