using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ITSMSkill.Proactive.Subscription
{
    public class SubscriptionStorage
    {
        public SubscriptionStorage(CosmosDbPartitionedStorageOptions cosmosDbPartitionedStorageOptions)
        {
            cosmosDbPartitionedStorageOptions.ContainerId = "subscriptions-collection";
            SubscriptionsStorage = new CosmosDbPartitionedStorage(cosmosDbPartitionedStorageOptions);
        }

        public IStorage SubscriptionsStorage { get; }
    }
}
