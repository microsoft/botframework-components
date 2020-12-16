// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Graph;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Microsoft.BotFramework.Composer.CustomAction.Actions.MSGraph
{
    /// <summary>
    /// Custom action for deleting an event in MS Graph
    /// </summary>
    [MsGraphCustomActionRegistration(DeleteEvent.DeleteEventDeclarativeType)]
    public class DeleteEvent : BaseMsGraphCustomAction
    {
        public const string DeleteEventDeclarativeType = "Microsoft.Graph.Calendar.DeleteEvent";

        [JsonConstructor]
        public DeleteEvent([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        [JsonProperty("eventId")]
        public StringExpression EventId { get; set; }

        public override string DeclarativeType => DeleteEventDeclarativeType;

        /// <summary>
        /// Deletes the event from Graph
        /// </summary>
        /// <param name="client"></param>
        /// <param name="dc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task CallGraphServiceAsync(GraphServiceClient client, DialogContext dc, CancellationToken cancellationToken)
        {
            var eventId = EventId.GetValue(dc.State);

            await client.Me.Events[eventId].Request().DeleteAsync(cancellationToken);
        }
    }
}
