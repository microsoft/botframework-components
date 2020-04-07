// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ITSMSkill.Dialogs.Teams
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveCards;
    using ITSMSkill.Extensions.Teams;
    using ITSMSkill.Extensions.Teams.TaskModule;
    using ITSMSkill.Models;
    using ITSMSkill.Models.ServiceNow;
    using ITSMSkill.Models.UpdateActivity;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Connector;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Schema.Teams;

    /// <summary>
    /// Class to update activities using activity reference object.
    /// </summary>
    public class UpdateActivityHelper
    {
        public static async Task UpdateTaskModuleActivityAsync(
            ITurnContext context,
            ActivityReference activityReference,
            Ticket ticketResponse,
            CancellationToken cancellationToken,
            bool isGroupTaskModule = false)
        {
            IConnectorClient connectorClient = context.TurnState.Get<IConnectorClient>()
                ?? throw new ArgumentNullException(nameof(ConnectorClient));

            Activity reply = context.Activity.CreateReply();
            reply.Attachments = new List<Microsoft.Bot.Schema.Attachment>
                {
                    new Microsoft.Bot.Schema.Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = GetThankYouCard(ticketResponse),
                    },
                };

            var teamsChannelActivity = reply.CreateConversationToTeamsChannel(
                new TeamsChannelData
                {
                    Channel = new ChannelInfo(id: activityReference.ThreadId),
                });

            await connectorClient.Conversations.UpdateActivityAsync(
                activityReference.ThreadId,
                activityReference.ActivityId,
                teamsChannelActivity,
                cancellationToken);
        }

        private static AdaptiveCard GetThankYouCard(Ticket ticketResponse)
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
                        TaskModuleFlowType = TeamsFlowType.CreateTicket_Form.ToString(),
                        Submit = true
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
                        TaskModuleFlowType = TeamsFlowType.CreateTicket_Form.ToString(),
                        Submit = true
                    }
                }
            });

            return card;
        }
    }
}
