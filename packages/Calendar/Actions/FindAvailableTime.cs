// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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

namespace Microsoft.Bot.Components.Calendar.Actions
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

            var results = new List<dynamic>();

            if (events != null)
            {
                foreach (var item in events)
                {
                    var evStart = DateTime.Parse(item.Start.DateTime);
                    var evEnd = DateTime.Parse(item.End.DateTime);

                    if (events.IndexOf(item) == 0)
                    {
                        if (evStart > workingTimeStart)
                        {
                            // if so, add free block to list from working hours start to start of first event
                            results.Add(new
                            {
                                type = "free",
                                value = new
                                {
                                    start = workingTimeStart,
                                    end = evStart,
                                    duration = evStart - workingTimeStart
                                }
                            });
                        }
                    }
                    else
                    {
                        // check if there is time between the previous event and the current one
                        if (evStart > results.Last().end && evEnd > results.Last().end)
                        {
                            // if so, add to list
                            results.Add(new
                            {
                                type = "free",
                                value = new
                                {
                                    start = results.Last().end,
                                    end = evStart,
                                    duration = results.Last().end - evStart
                                }
                            });
                        }
                    }

                    // add the event item
                    results.Add(new
                    {
                        type = "event",
                        start = evStart,
                        end = evEnd,
                        value = item,
                    });

                    if (events.IndexOf(item) == events.Count - 1)
                    {
                        if (evEnd < workingTimeEnd)
                        {
                            // if so, add free block to list from end of last meeting to end of working hours
                            results.Add(new
                            {
                                type = "free",
                                value = new
                                {
                                    start = evEnd,
                                    end = workingTimeEnd,
                                    duration = workingTimeEnd - evEnd
                                }
                            });
                        }
                    }
                }
            }

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
