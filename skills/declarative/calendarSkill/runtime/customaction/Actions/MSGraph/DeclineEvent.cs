﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Graph;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.BotFramework.Composer.CustomAction.Actions.MSGraph
{
    /// <summary>
    /// Custom action that calls MS Graph to decline an event
    /// </summary>
    [MsGraphCustomActionRegistration(DeclineEvent.DeclineEventDeclarativeType)]
    class DeclineEvent : BaseMsGraphCustomAction
    {
        /// <summary>
        /// Declarative type name fo this custom action, referenced by the Bot Composer
        /// </summary>
        public const string DeclineEventDeclarativeType = "Microsoft.Graph.Calendar.DeclineEvent";

        /// <summary>
        /// Creates an instance of <seealso cref="DeclineEvent" />
        /// </summary>
        /// <param name="callerPath"></param>
        /// <param name="callerLine"></param>
        [JsonConstructor]
        public DeclineEvent([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        /// <summary>
        /// ID of the event to decline
        /// </summary>
        /// <value></value>
        [JsonProperty("eventId")]
        public StringExpression EventId { get; set; }

        protected override string DeclarativeType => DeclineEventDeclarativeType;

        /// <summary>
        /// Calls the graph service to decline an event
        /// </summary>
        /// <param name="client"></param>
        /// <param name="dc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task CallGraphServiceAsync(GraphServiceClient client, DialogContext dc, CancellationToken cancellationToken)
        {
            var eventId = EventId.GetValue(dc.State);

            await client.Me.Events[eventId].Decline("decline").Request().PostAsync(cancellationToken);
        }
    }
}