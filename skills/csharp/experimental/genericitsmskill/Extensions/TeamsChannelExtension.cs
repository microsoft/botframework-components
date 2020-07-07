// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;

namespace GenericITSMSkill.Extensions
{
    public static class TeamsChannelExtension
    {
        public static Activity CreateConversationToTeamsChannel(this Activity activity, TeamsChannelData channelData)
        {
            var newActivity = new Activity
            {
                Type = ActivityTypes.Message,
                ChannelId = Channels.Msteams,
                ServiceUrl = string.Empty,
                From = new ChannelAccount(),
                Text = activity.Text,
                Speak = activity.Speak,
                InputHint = activity.InputHint,
                Attachments = activity.Attachments,
                Summary = activity.Summary
            };

            return newActivity;
        }
    }
}
