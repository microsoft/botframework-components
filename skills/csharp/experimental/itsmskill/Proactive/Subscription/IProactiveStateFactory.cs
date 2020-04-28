using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Proactive.Subscription
{
    public interface IProactiveStateFactory : IStateFactory
    {
        IStateManager SubscriptionState { get; }

        IStateManager ProactiveConversationState { get; }
    }
}
