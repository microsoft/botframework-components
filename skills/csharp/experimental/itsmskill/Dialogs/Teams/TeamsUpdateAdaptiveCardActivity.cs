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
    using ITSMSkill.Dialogs.Teams.CreateTicketTaskModuleView;
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
    public class TeamsUpdateAdaptiveCardActivity : ITeamsActivity<AdaptiveCard>
    {
        private readonly IConnectorClient _connectorClient;

        public TeamsUpdateAdaptiveCardActivity(IConnectorClient connectorClient)
        {
            _connectorClient = connectorClient;
        }

        public async Task<ResourceResponse> UpdateTaskModuleActivityAsync(
            ITurnContext context,
            ActivityReference activityReference,
            AdaptiveCard updateAdaptiveCard,
            CancellationToken cancellationToken)
        {
            Activity reply = context.Activity.CreateReply();
            reply.Attachments = new List<Microsoft.Bot.Schema.Attachment>
            {
                new Microsoft.Bot.Schema.Attachment
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = updateAdaptiveCard
                },
            };

            var teamsChannelActivity = reply.CreateConversationToTeamsChannel(
                new TeamsChannelData
                {
                    Channel = new ChannelInfo(id: activityReference.ThreadId),
                });

            var response = await _connectorClient.Conversations.UpdateActivityAsync(
                activityReference.ThreadId,
                activityReference.ActivityId,
                teamsChannelActivity,
                cancellationToken).ConfigureAwait(false);

            return response;
        }
    }
}
