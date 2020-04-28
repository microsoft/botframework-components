using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Proactive.Subscription
{
    public class UpdateSubscriptionResult
    {
        public UpdateSubscriptionResult(bool isSubscriptionDeleted, bool isSubscriptionCreated)
        {
            this.IsSubscriptionDeleted = isSubscriptionDeleted;
            this.IsSubscriptionCreated = isSubscriptionCreated;
        }

        public bool IsSubscriptionDeleted { get; }

        public bool IsSubscriptionCreated { get; }
    }
}
