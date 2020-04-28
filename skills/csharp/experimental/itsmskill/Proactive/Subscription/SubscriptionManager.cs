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
    using Microsoft.Extensions.DependencyInjection;

    public class SubscriptionManager
    {
        private readonly IProactiveStateFactory _proactiveStateFactory;
        private readonly ConversationState _conversationState;
        private readonly IStatePropertyAccessor<SkillState> _stateAccessor;

        public SubscriptionManager(IServiceProvider serviceProvider)
        {
            _conversationState = serviceProvider.GetService<ConversationState>();
            _stateAccessor = _conversationState.CreateProperty<SkillState>(nameof(SkillState));
            _proactiveStateFactory = serviceProvider.GetService<IProactiveStateFactory>();
            this.SubscriptionStateAccessor = Ensure.ArgIsNotNull(
                _proactiveStateFactory?.SubscriptionState?.CreateProperty<ProactiveSubscription>(),
                nameof(_proactiveStateFactory.SubscriptionState));
        }

        private IStateValueAccessor<ProactiveSubscription> SubscriptionStateAccessor { get; }
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
            ProactiveSubscription subscription = await this.GetSubscriptionByKey(context, subscriptionKey, cancellationToken);

            bool isNewSubscription = !subscription.ConversationReferences.Any();

            if (subscription.ConversationReferences.All(it => it.Conversation.Id != conversationReference.Conversation.Id))
            {
                subscription.ConversationReferences = new List<ConversationReference>(subscription.ConversationReferences.Append(conversationReference));
            }

            await this.SubscriptionStateAccessor.SetAsync(subscriptionKey, context, subscription, cancellationToken);

            return isNewSubscription;
        }

        /// <summary>
        /// Remove a conversation from a subscription.
        /// </summary>
        public async Task<bool> RemoveSubscription(
            ITurnContext turnContext,
            string subscriptionKey,
            ConversationReference conversationReference,
            CancellationToken cancellationToken)
        {
            bool isEmpty = false;

            ProactiveSubscription subscription = await this.SubscriptionStateAccessor.GetAsync(
                    subscriptionKey,
                    turnContext,
                    () => new ProactiveSubscription(),
                    cancellationToken);

            // remove thread message id
            string conversationId = conversationReference.Conversation.Id.Split(';')[0];

            List<ConversationReference> subscribedConversations = subscription.ConversationReferences?.ToList();
            subscribedConversations?.RemoveAll(it => it.Conversation.Id == conversationId);

            if (subscribedConversations == null || subscribedConversations.Count == 0)
            {
                await this.SubscriptionStateAccessor.DeleteAsync(subscriptionKey, turnContext, cancellationToken);
                isEmpty = true;
            }
            else
            {
                subscription.ConversationReferences = subscribedConversations;
                await this.SubscriptionStateAccessor.SetAsync(subscriptionKey, turnContext, subscription, cancellationToken);
            }

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

        public Task<ProactiveSubscription> GetSubscriptionByKey(
            ITurnContext context,
            string subscriptionKey,
            CancellationToken cancellationToken)
        {
            return this.SubscriptionStateAccessor.GetAsync(
                subscriptionKey,
                context,
                () => new ProactiveSubscription(),
                cancellationToken);
        }
    }
}
