using ITSMSkill.Models;
using ITSMSkill.Models.UpdateActivity;
using ITSMSkill.Utilities;
using Microsoft.Bot.Builder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Proactive.Subscription
{
    public static class ProactiveSkilStateFactoryExtensions
    {
        public static IStatePropertyAccessor<ConversationSubscriptions<T>> GetSubscriptionMapAccessor<T>(this IProactiveStateFactory skillStateFactory)
        {
            Ensure.ArgIsNotNull(skillStateFactory, nameof(skillStateFactory));

            return skillStateFactory.ConversationState
                .CreateProperty<ConversationSubscriptions<T>>(nameof(ConversationSubscriptions<T>));
        }

        public static IStatePropertyAccessor<ActivityReferenceMap> GetActivityReferenceMapAccessor(this IProactiveStateFactory skillStateFactory)
        {
            Ensure.ArgIsNotNull(skillStateFactory, nameof(skillStateFactory));

            return skillStateFactory.ConversationState.CreateProperty<ActivityReferenceMap>(nameof(ActivityReferenceMap));
        }
    }
}
