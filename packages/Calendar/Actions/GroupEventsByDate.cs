// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Components.Graph.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Components.Calendar.Actions
{
    public class GroupEventsByDate : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.GroupEventsByDate";

        [JsonConstructor]
        public GroupEventsByDate([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public StringExpression ResultProperty { get; set; }

        /// <summary>
        /// Gets or sets the start time of the datetime range
        /// </summary>
        /// <value>The start time of the datetime range </value>
        [JsonProperty("startProperty")]
        public ObjectExpression<DateTime?> StartProperty { get; set; }

        /// <summary>
        /// Gets or sets the end of the datetime range
        /// </summary>
        /// <value>The end of the datetime range</value>
        [JsonProperty("endProperty")]
        public ObjectExpression<DateTime?> EndProperty { get; set; }

        public ArrayExpression<CalendarSkillEventModel> EventsProperty { get; set; }

        public async override Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var eventsProperty = EventsProperty.GetValue(dcState);
            var startProperty = this.StartProperty.GetValue(dcState);
            var endProperty = this.EndProperty.GetValue(dcState);

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
                    events = eventsProperty.Where(ev => ev.IsAllDay == false && DateTime.Parse(ev.Start.DateTime).Date == dt.Date).ToArray()
                });
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(DeclarativeType, groupedEvents, valueType: DeclarativeType, label: DeclarativeType).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dc.State.SetValue(this.ResultProperty.GetValue(dc.State), JToken.FromObject(groupedEvents));
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: groupedEvents, cancellationToken: cancellationToken);
        }
    }
}
