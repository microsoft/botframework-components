using AdaptiveCards;
using ITSMSkill.Extensions.Teams;
using ITSMSkill.Extensions.Teams.TaskModule;
using ITSMSkill.Models.UpdateActivity;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ITSMSkill.Dialogs.Teams
{
    public class UpdateActivityHelper
    {
        public static async Task UpdateTaskModuleActivityAsync(
            ITurnContext context,
            ActivityReference activityReference,
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
                        Content = GetThankYouCard(),
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

        private static AdaptiveCard GetThankYouCard()
        {
            AdaptiveCard card = new AdaptiveCard("1.0");

            var list = new List<AdaptiveElement>
            {
                new AdaptiveTextBlock
                {
                    Text = "Thank you",
                    Weight = AdaptiveTextWeight.Bolder,
                    Size = AdaptiveTextSize.Medium,
                    Color = AdaptiveTextColor.Accent,
                },
            };

            // Add Adaptive Elements
            card.Body.AddRange(list);

            return card;
        }
    }
}
