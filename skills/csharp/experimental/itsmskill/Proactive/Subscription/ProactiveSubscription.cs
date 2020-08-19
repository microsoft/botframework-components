namespace ITSMSkill.Proactive.Subscription
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Bot.Schema;

    public class ProactiveSubscription
    {
        public ProactiveSubscription()
        {
            ConversationReferences = new List<ConversationReference>();
        }

        /// <summary>
        /// Gets or sets the list of <see cref="ConversationReference"/>
        /// associated with the event.
        /// </summary>
        public IEnumerable<ConversationReference> ConversationReferences { get; set; }

        public bool IsChannelSubscribed(ConversationReference channelReference)
        {
            return this.ConversationReferences?
                       .Any(channel => channel.Conversation.Id == channelReference.Conversation.Id) == true;
        }
    }
}
