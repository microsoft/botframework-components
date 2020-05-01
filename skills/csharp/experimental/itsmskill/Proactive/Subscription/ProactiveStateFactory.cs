using Microsoft.Bot.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Proactive.Subscription
{
    public class ProactiveStateFactory : IProactiveStateFactory
    {
        private readonly SubscriptionStorage _subscriptionStorage;
        private readonly ConversationState _conversationState;
        private readonly IStorage _storage;

        public ProactiveStateFactory(IServiceProvider serviceProvider)
        {
            _conversationState = serviceProvider.GetService<ConversationState>();
            _storage = serviceProvider.GetService<IStorage>();
            _subscriptionStorage = serviceProvider.GetService<SubscriptionStorage>();

            this.SubscriptionState = new StateAccessor(_subscriptionStorage.SubscriptionsStorage, nameof(ProactiveSubscription));
        }

        // TODO Check how we can make AutoSaveStateMiddleware to save this state along with the states defined in the StateSet...
        public IStateManager SubscriptionState { get; private set; }

        public IStateManager ProactiveConversationState { get; }
        public BotStateSet StateSet { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public IStorage Storage => throw new NotImplementedException();

        public UserState UserState => throw new NotImplementedException();

        public ConversationState ConversationState => throw new NotImplementedException();

        public ChannelState ChannelState { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}
