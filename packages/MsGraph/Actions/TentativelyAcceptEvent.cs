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
    /// Custom action to Graph that allows the caller to tentatively accept an invite
    /// </summary>
    [MsGraphCustomActionRegistration(TentativelyAcceptEvent.TentativelyAcceptEventDeclarativeType)]
    public class TentativelyAcceptEvent : BaseMsGraphCustomAction
    {
        public const string TentativelyAcceptEventDeclarativeType = "Microsoft.Graph.Calendar.TentativelyAcceptEvent";

        [JsonConstructor]
        public TentativelyAcceptEvent([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        /// <summary>
        /// ID of the event of tentatively accept
        /// </summary>
        /// <value></value>
        [JsonProperty("eventId")]
        public StringExpression EventId { get; set; }

        public override string DeclarativeType => TentativelyAcceptEventDeclarativeType;

        /// <summary>
        /// Calls the graph service to tentatively accept the event on the user's behalf
        /// </summary>
        /// <param name="client"></param>
        /// <param name="dc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task CallGraphServiceAsync(GraphServiceClient client, DialogContext dc, CancellationToken cancellationToken)
        {
            var eventId = this.EventId.GetValue(dc.State);

            await client.Me.Events[eventId].TentativelyAccept("tentativelyAccept").Request().PostAsync();
        }
    }
}
