// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;

namespace ITSMSkill.Extensions.Teams
{
    /// <summary>
    /// class for creating ConversationForTeamsChannel.
    /// </summary>
    public static class TeamsChannelExtension
    {
        public static Activity CreateConversationToTeamsChannel(this Activity activity, TeamsChannelData channelData)
        {
            var newActivity = new Activity
            {
                Type = ActivityTypes.Message,
                ChannelId = Channels.Msteams,
                ServiceUrl = activity.ServiceUrl,
                From = activity.Recipient,
                Text = activity.Text,
                Speak = activity.Speak,
                InputHint = activity.InputHint,
                Attachments = activity.Attachments,
                Summary = activity.Summary,
                ChannelData = new TeamsChannelData
                {
                    Channel = channelData.Channel,
                    Team = channelData.Team,
                    Tenant = channelData.Tenant
                },
            };

            return newActivity;
        }
    }
}
