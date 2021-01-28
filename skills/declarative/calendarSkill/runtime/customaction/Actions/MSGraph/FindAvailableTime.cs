// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.BotFramework.Composer.CustomAction.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.BotFramework.Composer.CustomAction.Actions.MSGraph
{
    /// <summary>
    /// This action calls the Microsoft.Recognizers.Text library for recognizing DateTimes from strings. 
    /// This has proven to be more consistent that LUIS datetime recognition by allowing the use of the 
    /// user's current timezone time as a relative datetime rather than a mix of absolute times and UTC times.
    /// </summary>
    public class FindAvailableTime : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.FindAvailableTime";

        [JsonConstructor]
        public FindAvailableTime([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }
        
        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("inputProperty")]
        public ObjectExpression<List<CalendarSkillEventModel>> InputProperty { get; set; }

        [JsonProperty("workingHourStartProperty")]
        public ObjectExpression<DateTime?> WorkingHourStartProperty { get; set; }

        [JsonProperty("workingHourEndProperty")]
        public ObjectExpression<DateTime?> WorkingHourEndProperty { get; set; }

        [JsonProperty("startProperty")]
        public ObjectExpression<DateTime?> StartProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var events = InputProperty.GetValue(dcState);
            var startProperty = StartProperty.GetValue(dcState);
            var workingHourStartProperty = WorkingHourStartProperty.GetValue(dcState);
            var workingHourEndProperty = WorkingHourEndProperty.GetValue(dcState);
            DateTime workingTimeStart = startProperty.Value.Date + workingHourStartProperty.Value.TimeOfDay;
            DateTime workingTimeEnd = startProperty.Value.Date + workingHourEndProperty.Value.TimeOfDay;
            DateTime availableTimeStart = startProperty.Value > workingTimeStart ? startProperty.Value : workingTimeStart;
            DateTime availableTimeEnd = workingTimeEnd;
            CalendarSkillEventModel previousEvent = null;
            CalendarSkillEventModel nextEvent = null;

            if (events != null)
            {
                foreach (var ev in events)
                {
                    var start = DateTime.Parse(ev.Start.DateTime);
                    var end = DateTime.Parse(ev.End.DateTime);
                    if (start > availableTimeStart)
                    {
                        nextEvent = ev;
                        availableTimeEnd = start;
                        break;
                    }
                    else
                    {
                        previousEvent = ev;
                        availableTimeStart = end;
                    }
                }
            }

            var results = new
            {
                previousEvent,
                nextEvent,
                availableTimeStart,
                availableTimeEnd,
                availableTimeLength = availableTimeEnd > availableTimeStart ? (int)(availableTimeEnd - availableTimeStart).TotalMinutes : 0
            };

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(DeclarativeType, results, valueType: DeclarativeType, label: DeclarativeType).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, results);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: results, cancellationToken: cancellationToken);
        }
    }
}
