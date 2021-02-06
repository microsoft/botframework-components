// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.MsGraph.Actions.MSGraph
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveExpressions.Properties;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Component.MsGraph.Models;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// This action gets an event from MS Graph by its EventId.
    /// </summary>
    [MsGraphCustomActionRegistration(GetEventById.GetEventByIdDeclarativeType)]
    public class GetEventById : BaseMsGraphCustomAction<CalendarSkillEventModel>
    {
        /// <summary>
        /// Declarative type name for this custom action
        /// </summary>
        public const string GetEventByIdDeclarativeType = "Microsoft.Graph.Calendar.GetEventById";

        /// <summary>
        /// Creates an instance of <seealso cref="GetEventById" />
        /// </summary>
        /// <param name="callerPath"></param>
        /// <param name="callerLine"></param>
        [JsonConstructor]
        public GetEventById([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        /// <summary>
        /// Gets or sets the id of the event
        /// </summary>
        /// <value>Id of the event</value>
        [JsonProperty("eventIdProperty")]
        public StringExpression EventIdProperty { get; set; }

        /// <summary>
        /// Gets or sets the timezone of the event
        /// </summary>
        /// <value>Timezone of the event</value>
        [JsonProperty("timeZoneProperty")]
        public StringExpression TimeZoneProperty { get; set; }

        /// <summary>
        /// Declarative type of this custom action
        /// </summary>
        public override string DeclarativeType => GetEventByIdDeclarativeType;

        /// <summary>
        /// Gets the event by its id from the Graph service
        /// </summary>
        /// <param name="client"></param>
        /// <param name="dc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<CalendarSkillEventModel> CallGraphServiceWithResultAsync(GraphServiceClient client, DialogContext dc, CancellationToken cancellationToken)
        {
            var dcState = dc.State;
            var eventId = this.EventIdProperty.GetValue(dcState);
            var timeZoneProperty = this.TimeZoneProperty.GetValue(dcState);
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneProperty);

            Event ev = await client.Me.Events[eventId].Request().GetAsync(cancellationToken);

            var currentDateTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, timeZone);
            var startTZ = TimeZoneInfo.ConvertTimeFromUtc(DateTime.Parse(ev.Start.DateTime), timeZone);
            var endTZ = TimeZoneInfo.ConvertTimeFromUtc(DateTime.Parse(ev.End.DateTime), timeZone);

            ev.Start = DateTimeTimeZone.FromDateTime(startTZ, timeZone);
            ev.End = DateTimeTimeZone.FromDateTime(endTZ, timeZone);

            return new CalendarSkillEventModel(ev, TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone));
        }
    }
}
