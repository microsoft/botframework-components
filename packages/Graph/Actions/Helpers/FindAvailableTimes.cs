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

    /// <summary>
    /// This action accepts a collection of events and identifies any breaks between those meetings.
    /// It returns a combined collection of events and free blocks.
    /// </summary>
    [GraphCustomActionRegistration(FindAvailableTimes.DeclarativeType)]
    public class FindAvailableTimes : Dialog
    {
        /// <summary>
        /// The declarative type name for this action.
        /// </summary>
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.Helpers.FindAvailableTimes";

        /// <summary>
        /// Initializes a new instance of the <see cref="FindAvailableTimes"/> class.
        /// </summary>
        /// <param name="callerPath">The path of the caller.</param>
        /// <param name="callerLine">The line number at which the method is called.</param>
        [JsonConstructor]
        public FindAvailableTimes([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        /// <summary>
        /// Gets or sets the property name where the result of the action should be stored.
        /// </summary>
        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        /// <summary>
        /// Gets or sets the events property.
        /// </summary>
        [JsonProperty("events")]
        public ArrayExpression<CalendarSkillEventModel> Events { get; set; }

        /// <summary>
        /// Gets or sets the property for the start of the working hours.
        /// </summary>
        [JsonProperty("workingHourStart")]
        public ObjectExpression<DateTime?> WorkingHourStart { get; set; }

        /// <summary>
        /// Gets or sets the property for the end of the working hours.
        /// </summary>
        [JsonProperty("workingHourEnd")]
        public ObjectExpression<DateTime?> WorkingHourEnd { get; set; }

        /// <summary>
        /// Gets or sets the property for the start date.
        /// </summary>
        [JsonProperty("start")]
        public ObjectExpression<DateTime?> Start { get; set; }

        /// <inheritdoc/>
        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var events = this.Events.GetValue(dcState);
            var startProperty = this.Start.GetValue(dcState);
            var workingHourStartProperty = this.WorkingHourStart.GetValue(dcState);
            var workingHourEndProperty = this.WorkingHourEnd.GetValue(dcState);
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
                                    duration = evStart - workingTimeStart,
                                },
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
                                    duration = results.Last().end - evStart,
                                },
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
                                    duration = workingTimeEnd - evEnd,
                                },
                            });
                        }
                    }
                }
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(DeclarativeType, results, valueType: DeclarativeType, label: DeclarativeType).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(this.ResultProperty, results);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: results, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
