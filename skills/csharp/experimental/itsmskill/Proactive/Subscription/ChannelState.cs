namespace ITSMSkill.Proactive.Subscription
{
    using System;
    using System.Text.RegularExpressions;
    using Microsoft.Bot.Builder;

    public class ChannelState : BotState
    {
        public ChannelState(IStorage storage)
            : base(storage, nameof(ChannelState))
        {
        }

        protected override string GetStorageKey(ITurnContext turnContext)
        {
            string channelId = turnContext.Activity.ChannelId
                               ?? throw new ArgumentNullException(nameof(turnContext.Activity.ChannelId), "invalid activity-missing channelId");
            string conversationId = turnContext.Activity.Conversation?.Id
                                    ?? throw new ArgumentNullException(nameof(turnContext.Activity.ChannelId), "invalid activity-missing Conversation.Id");

            conversationId = turnContext.Activity.ChannelId == "msteams" && turnContext.Activity.Conversation.IsGroup == true
                ? Regex.Replace(conversationId, @"(;messageid=\d+$)", string.Empty)
                : conversationId;

            return $"{channelId}/channels/{conversationId}";
        }
    }
}
