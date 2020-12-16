namespace ITSMSkill.Proactive.Subscription
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ITSMSkill.Models;
    using ITSMSkill.Utilities;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Solutions.Proactive;
    using Microsoft.Extensions.DependencyInjection;

    public class SubscriptionManager
    {
        private readonly ConversationState _conversationState;
        private readonly IStatePropertyAccessor<ProactiveSubscriptionMap> _proactiveStateSubscription;
        private readonly ProactiveState _proactiveState;

        public SubscriptionManager(IServiceProvider serviceProvider)
        {
            _conversationState = serviceProvider.GetService<ConversationState>();
            _proactiveState = serviceProvider.GetService<ProactiveState>();
            _proactiveStateSubscription = _proactiveState.CreateProperty<ProactiveSubscriptionMap>(nameof(ProactiveSubscriptionMap));
        }

        /// <summary>
        /// Add a conversation to a subscription.
        /// </summary>
        /// <param name="context">The turn context.</param>
        /// <param name="subscriptionKey">The Subscription key.</param>
        /// <param name="conversationReference">Conversation reference.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<bool> AddSubscription(
            ITurnContext context,
            string subscriptionKey,
            ConversationReference conversationReference,
            CancellationToken cancellationToken)
        {
            bool isNewSubscription = false;
            ProactiveSubscription subscription = await this.GetSubscriptionByKey(context, subscriptionKey, cancellationToken);
            var proactiveSubscriptionMap = await this._proactiveStateSubscription.GetAsync(context, () => new ProactiveSubscriptionMap()).ConfigureAwait(false);

            if (subscription != null)
            {
                proactiveSubscriptionMap[subscriptionKey] = new ProactiveSubscription
                {
                    ConversationReferences = new List<ConversationReference>(subscription.ConversationReferences.Append(conversationReference))
                };
            }
            else
            {
                proactiveSubscriptionMap[subscriptionKey] = new ProactiveSubscription
                {
                    ConversationReferences = new List<ConversationReference> { conversationReference }
                };

                isNewSubscription = true;
            }

            await this._proactiveStateSubscription.SetAsync(context, proactiveSubscriptionMap, cancellationToken);

            // Save ProactiveState
            await _proactiveState.SaveChangesAsync(context, false, cancellationToken);

            return isNewSubscription;
        }

        /// <summary>
        /// Remove a conversation from a subscription.
        /// </summary>
        public async Task<bool> RemoveSubscription(
            ITurnContext context,
            string subscriptionKey,
            ConversationReference conversationReference,
            CancellationToken cancellationToken)
        {
            bool isEmpty = false;

            ProactiveSubscription subscription = await this.GetSubscriptionByKey(context, subscriptionKey, cancellationToken);

            // Get SubscriptionMap
            var proactiveSubscriptionMap = await this._proactiveStateSubscription.GetAsync(context, () => new ProactiveSubscriptionMap()).ConfigureAwait(false);

            // remove thread message id
            string conversationId = conversationReference.Conversation.Id.Split(';')[0];

            List<ConversationReference> subscribedConversations = subscription.ConversationReferences?.ToList();
            subscribedConversations?.RemoveAll(it => it.Conversation.Id == conversationId);

            if (subscribedConversations == null || subscribedConversations.Count == 0)
            {
                await this._proactiveStateSubscription.SetAsync(context, null, cancellationToken);
                isEmpty = true;
            }
            else
            {
                subscription.ConversationReferences = subscribedConversations;
                proactiveSubscriptionMap[subscriptionKey] = subscription;
                await this._proactiveStateSubscription.SetAsync(context, proactiveSubscriptionMap, cancellationToken);       }

            return isEmpty;
        }

        public async Task<UpdateSubscriptionResult> UpdateSubscription(
            ITurnContext turnContext,
            string oldSubscriptionKey,
            string newSubscriptionKey,
            ConversationReference conversationReference,
            CancellationToken cancellationToken)
        {
            // remove subscription
            var isSubscriptionEmpty = await this.RemoveSubscription(turnContext, oldSubscriptionKey, conversationReference, cancellationToken);

            // add subscription
            var isSubscriptionCreated = await this.AddSubscription(turnContext, newSubscriptionKey, conversationReference, cancellationToken);

            return new UpdateSubscriptionResult(isSubscriptionEmpty, isSubscriptionCreated);
        }

        public async Task<ProactiveSubscription> GetSubscriptionByKey(
            ITurnContext context,
            string subscriptionKey,
            CancellationToken cancellationToken)
        {
            var proactiveSubscriptionMap = await this._proactiveStateSubscription.GetAsync(context, () => new ProactiveSubscriptionMap()).ConfigureAwait(false);
            proactiveSubscriptionMap.TryGetValue(subscriptionKey, out var proactiveSubscription);
            return proactiveSubscription ?? null;
        }
    }
}
