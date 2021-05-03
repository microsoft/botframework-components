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
    /// Represents a custom action that calls to MSGraph to accept an event.
    /// </summary>
    [GraphCustomActionRegistration(AcceptEvent.AcceptEventDeclarativeType)]
    public class AcceptEvent : BaseMsGraphCustomAction
    {
        /// <summary>
        /// The declarative type of the custom action.
        /// </summary>
        private const string AcceptEventDeclarativeType = "Microsoft.Graph.Calendar.AcceptEvent";

        /// <summary>
        /// Initializes a new instance of the <see cref="AcceptEvent"/> class.
        /// </summary>
        /// <param name="callerPath">Caller path.</param>
        /// <param name="callerLine">Caller line.</param>
        [JsonConstructor]
        public AcceptEvent([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        /// <summary>
        /// Gets or sets the event ID in which to accept.
        /// </summary>
        [JsonProperty("eventId")]
        public StringExpression EventId { get; set; }

        /// <inheritdoc/>
        public override string DeclarativeType => AcceptEvent.AcceptEventDeclarativeType;

        /// <inheritdoc/>
        internal override async Task CallGraphServiceAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            await client.Me.Events[(string)parameters["EventId"]].Accept("accept").Request().PostAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override void PopulateParameters(DialogStateManager state, Dictionary<string, object> parameters)
        {
            parameters.Add("EventId", this.EventId.GetValue(state));
        }
    }
}
