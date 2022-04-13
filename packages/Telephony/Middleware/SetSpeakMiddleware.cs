// ---------------------------------------------------------------------------
// <copyright file="SetSpeakMiddleware.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Components.Telephony.Middleware
{
    /// <summary>
    /// The middleware that handles all outgoing messages
    /// </summary>
    public class SetSpeakMiddleware : IMiddleware
    {

        public delegate void EventReceiverHandler(ITurnContext turnContext);
        private Dictionary<string, List<EventReceiverHandler>> _receivers;

        /// <summary>
        /// Initializes a new SetSpeakMiddleware class
        /// </summary>
        public SetSpeakMiddleware()
        {
            _receivers = new Dictionary<string, List<EventReceiverHandler>>();
        }

        /// <summary>
        /// Handles the outgoing message
        /// </summary>
        /// <param name="turnContext">The turn context</param>
        /// <param name="next">The next delegate</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <returns></returns>
        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(turnContext.Activity?.Type) && _receivers.TryGetValue(turnContext.Activity.Type, out var receivers))
                receivers.ForEach(rc => rc.Invoke(turnContext));
            
            await next(cancellationToken);
        }

        public void addEventReceiver(string eventName, EventReceiverHandler handler)
        {
            if (!_receivers.ContainsKey(eventName)) _receivers.Add(eventName, new List<EventReceiverHandler>());
            _receivers[eventName].Add(handler);
        }

        public void removeEventReceiver(string eventName, EventReceiverHandler handler)
        {
            if (!_receivers.ContainsKey(eventName)) return;
            _receivers[eventName].Remove(handler);
        }

    }
}
