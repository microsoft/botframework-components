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
    /// Custom action for deleting an event in MS Graph.
    /// </summary>
    [GraphCustomActionRegistration(DeleteEvent.DeleteEventDeclarativeType)]
    public class DeleteEvent : BaseMsGraphCustomAction
    {
        private const string DeleteEventDeclarativeType = "Microsoft.Graph.Calendar.DeleteEvent";

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteEvent"/> class.
        /// </summary>
        /// <param name="callerPath">Caller path.</param>
        /// <param name="callerLine">Caller line.</param>
        [JsonConstructor]
        public DeleteEvent([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        /// <summary>
        /// Gets or sets the event ID to delete.
        /// </summary>
        [JsonProperty("eventId")]
        public StringExpression EventId { get; set; }

        /// <inheritdoc/>
        public override string DeclarativeType => DeleteEventDeclarativeType;

        /// <inheritdoc/>
        internal override async Task CallGraphServiceAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            await client.Me.Events[(string)parameters["EventId"]].Request().DeleteAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override void PopulateParameters(DialogStateManager state, Dictionary<string, object> parameters)
        {
            parameters.Add("EventId", this.EventId.GetValue(state));
        }
    }
}
