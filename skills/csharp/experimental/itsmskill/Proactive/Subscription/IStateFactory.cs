namespace ITSMSkill.Proactive.Subscription
{
    using Microsoft.Bot.Builder;

    public interface IStateFactory
    {
        BotStateSet StateSet { get; set; }

        IStorage Storage { get; }

        UserState UserState { get; }

        ConversationState ConversationState { get; }

        ChannelState ChannelState { get; set; }
    }
}
