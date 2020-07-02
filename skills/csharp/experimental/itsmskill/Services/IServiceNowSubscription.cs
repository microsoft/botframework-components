using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Services
{
    public interface IServiceNowSubscription
    {
        Task<string> CreateSubscriptionBusinessRule(string urgencyFilter, string filterName, string notificationNameSpace = null, string postNotificationAPIName = null);

        Task RemoveSubscriptionBusinessRule(string subscriptionId);

        Task CreateSubscriptionRestMessages(string messageName, string url);

        Task RemoveSubscriptionRestMessage(string messageName);
    }
}
