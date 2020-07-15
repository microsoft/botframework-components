using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ITSMSkill.Services
{
    public interface IServiceNowBusinessRuleSubscription
    {
        Task<HttpStatusCode> CreateSubscriptionBusinessRule(string urgencyFilter, string filterName, string notificationNameSpace = null, string postNotificationAPIName = null);

        Task RemoveSubscriptionBusinessRule(string subscriptionId);

        Task CreateSubscriptionRestMessages(string messageName, string url);

        Task RemoveSubscriptionRestMessage(string messageName);

        Task<HttpStatusCode> CreateNewRestMessage(string callBackName, string postName);
    }
}
