// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.MsGraph.Actions.MSGraph
{
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveExpressions.Properties;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// Represents a custom action that calls to MSGraph to accept an event
    /// </summary>
    [MsGraphCustomActionRegistration(AcceptEvent.AcceptEventDeclarativeType)]
    public class AcceptEvent : BaseMsGraphCustomAction
    {
        /// <summary>
        /// The declarative type of the custom action
        /// </summary>
        public const string AcceptEventDeclarativeType = "Microsoft.Graph.Calendar.AcceptEvent";

        /// <summary>
        /// Creates an instance of <seealso cref="AcceptEvent" />
        /// </summary>
        /// <param name="callerPath"></param>
        /// <param name="callerLine"></param>
        [JsonConstructor]
        public AcceptEvent([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        /// <summary>
        /// Event ID in which to accept
        /// </summary>
        /// <value></value>
        [JsonProperty("eventId")]
        public StringExpression EventId { get; set; }

        public override string DeclarativeType => AcceptEvent.AcceptEventDeclarativeType;

        protected override async Task CallGraphServiceAsync(GraphServiceClient client, DialogContext dc, CancellationToken cancellationToken)
        {
            var dcState = dc.State;
            var eventId = this.EventId.GetValue(dcState);

            await client.Me.Events[eventId].Accept("accept").Request().PostAsync(cancellationToken);
        }
    }
}
