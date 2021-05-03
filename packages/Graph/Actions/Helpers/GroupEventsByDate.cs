// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Components.Graph.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveExpressions.Properties;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.TraceExtensions;
    using Microsoft.Bot.Components.Graph.Models;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// This action accepts a collection of events and returns a collection of events group by date.
    /// </summary>
    [GraphCustomActionRegistration(GroupEventsByDate.DeclarativeType)]
    public class GroupEventsByDate : Dialog
    {
        /// <summary>
        /// The declarative type name for this action.
        /// </summary>
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.Helpers.GroupEventsByDate";

        /// <summary>
        /// Initializes a new instance of the <see cref="GroupEventsByDate"/> class.
        /// </summary>
        /// <param name="callerPath">The path of the caller.</param>
        /// <param name="callerLine">The line number at which the method is called.</param>
        [JsonConstructor]
        public GroupEventsByDate([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        /// <summary>
        /// Gets or sets the property name where the result of the action should be stored.
        /// </summary>
        [JsonProperty("resultProperty")]
        public StringExpression ResultProperty { get; set; }

        /// <summary>
        /// Gets or sets the start time of the datetime range.
        /// </summary>
        /// <value>The start time of the datetime range. </value>
        [JsonProperty("start")]
        public ObjectExpression<DateTime?> Start { get; set; }

        /// <summary>
        /// Gets or sets the end of the datetime range.
        /// </summary>
        /// <value>The end of the datetime range.</value>
        [JsonProperty("end")]
        public ObjectExpression<DateTime?> End { get; set; }

        /// <summary>
        /// Gets or sets the array of events to be sorted.
        /// </summary>
        [JsonProperty("events")]
        public ArrayExpression<CalendarSkillEventModel> Events { get; set; }

        /// <inheritdoc/>
        public async override Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var eventsProperty = this.Events.GetValue(dcState);
            var startProperty = this.Start.GetValue(dcState);
            var endProperty = this.End.GetValue(dcState);

            if (endProperty == null)
            {
                endProperty = startProperty.Value.Date.AddHours(23).AddMinutes(59);
            }

            var groupedEvents = new List<object>();

            for (var dt = startProperty.Value; dt <= endProperty.Value; dt = dt.AddDays(1))
            {
                groupedEvents.Add(new
                {
                    date = dt,
                    events = eventsProperty.Where(ev => ev.IsAllDay == false && DateTime.Parse(ev.Start.DateTime).Date == dt.Date).ToArray(),
                });
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(DeclarativeType, groupedEvents, valueType: DeclarativeType, label: DeclarativeType).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dc.State.SetValue(this.ResultProperty.GetValue(dc.State), JToken.FromObject(groupedEvents));
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: groupedEvents, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
