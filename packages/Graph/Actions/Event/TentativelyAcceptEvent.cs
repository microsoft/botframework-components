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
    /// Tentatively accept an event.
    /// </summary>
    [GraphCustomActionRegistration(TentativelyAcceptEvent.TentativelyAcceptEventDeclarativeType)]
    public class TentativelyAcceptEvent : BaseMsGraphCustomAction
    {
        private const string TentativelyAcceptEventDeclarativeType = "Microsoft.Graph.Calendar.TentativelyAcceptEvent";

        /// <summary>
        /// Initializes a new instance of the <see cref="TentativelyAcceptEvent"/> class.
        /// </summary>
        /// <param name="callerPath">Caller path.</param>
        /// <param name="callerLine">Caller line.</param>
        [JsonConstructor]
        public TentativelyAcceptEvent([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        /// <summary>
        /// Gets or sets the ID of the event of tentatively accept.
        /// </summary>
        [JsonProperty("eventId")]
        public StringExpression EventId { get; set; }

        /// <inheritdoc/>
        public override string DeclarativeType => TentativelyAcceptEventDeclarativeType;

        /// <inheritdoc/>
        internal override async Task CallGraphServiceAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            var eventId = (string)parameters["EventId"];

            await client.Me.Events[eventId].TentativelyAccept("tentativelyAccept").Request().PostAsync().ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override void PopulateParameters(DialogStateManager state, Dictionary<string, object> parameters)
        {
            parameters.Add("EventId", this.EventId.GetValue(state));
        }
    }
}
