using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
