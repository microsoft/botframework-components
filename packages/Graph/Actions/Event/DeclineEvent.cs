// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.Graph.Actions
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveExpressions.Properties;
    using Microsoft.Bot.Builder.Dialogs.Memory;
    using Microsoft.Bot.Components.Graph;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// Custom action that calls MS Graph to decline an event.
    /// </summary>
    [GraphCustomActionRegistration(DeclineEvent.DeclineEventDeclarativeType)]
    public class DeclineEvent : BaseMsGraphCustomAction
    {
        /// <summary>
        /// Declarative type name fo this custom action, referenced by the Bot Composer.
        /// </summary>
        private const string DeclineEventDeclarativeType = "Microsoft.Graph.Calendar.DeclineEvent";

        /// <summary>
        /// Initializes a new instance of the <see cref="DeclineEvent"/> class.
        /// </summary>
        /// <param name="callerPath">Caller path.</param>
        /// <param name="callerLine">Caller line.</param>
        public DeclineEvent([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        /// <summary>
        /// Gets or sets the ID of the event to decline.
        /// </summary>
        [JsonProperty("eventId")]
        public StringExpression EventId { get; set; }

        /// <inheritdoc/>
        public override string DeclarativeType => DeclineEventDeclarativeType;

        /// <inheritdoc/>
        internal override async Task CallGraphServiceAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            await client.Me.Events[(string)parameters["EventId"]].Decline("decline").Request().PostAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override void PopulateParameters(DialogStateManager state, Dictionary<string, object> parameters)
        {
            parameters.Add("EventId", this.EventId.GetValue(state));
        }
    }
}