// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.Graph.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveExpressions.Properties;
    using Microsoft.Bot.Builder.Dialogs.Memory;
    using Microsoft.Bot.Components.Graph;
    using Microsoft.Bot.Components.Graph.Models;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// This action gets an event from MS Graph by its EventId.
    /// </summary>
    [GraphCustomActionRegistration(GetEventById.GetEventByIdDeclarativeType)]
    public class GetEventById : BaseMsGraphCustomAction<CalendarSkillEventModel>
    {
        /// <summary>
        /// Declarative type name for this custom action.
        /// </summary>
        private const string GetEventByIdDeclarativeType = "Microsoft.Graph.Calendar.GetEventById";

        /// <summary>
        /// Initializes a new instance of the <see cref="GetEventById"/> class.
        /// </summary>
        /// <param name="callerPath">Caller path.</param>
        /// <param name="callerLine">Caller line.</param>
        [JsonConstructor]
        public GetEventById([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        /// <summary>
        /// Gets or sets the id of the event.
        /// </summary>
        [JsonProperty("eventId")]
        public StringExpression EventId { get; set; }

        /// <summary>
        /// Gets or sets the timezone of the event.
        /// </summary>
        [JsonProperty("timeZone")]
        public StringExpression TimeZone { get; set; }

        /// <inheritdoc/>
        public override string DeclarativeType => GetEventByIdDeclarativeType;

        /// <inheritdoc/>
        internal override async Task<CalendarSkillEventModel> CallGraphServiceWithResultAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            var timeZone = GraphUtils.ConvertTimeZoneFormat((string)parameters["Timezone"]);

            Event ev = await client.Me.Events[(string)parameters["EventId"]].Request().GetAsync(cancellationToken).ConfigureAwait(false);

            var startTZ = TimeZoneInfo.ConvertTimeFromUtc(DateTime.Parse(ev.Start.DateTime), timeZone);
            var endTZ = TimeZoneInfo.ConvertTimeFromUtc(DateTime.Parse(ev.End.DateTime), timeZone);

            ev.Start = DateTimeTimeZone.FromDateTime(startTZ, timeZone);
            ev.End = DateTimeTimeZone.FromDateTime(endTZ, timeZone);

            return new CalendarSkillEventModel(ev, TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone));
        }

        /// <inheritdoc/>
        protected override void PopulateParameters(DialogStateManager state, Dictionary<string, object> parameters)
        {
            parameters.Add("EventId", this.EventId.GetValue(state));
            parameters.Add("Timezone", this.TimeZone.GetValue(state));
        }
    }
}
