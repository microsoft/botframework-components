// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.MsGraph.Actions.MSGraph
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveExpressions.Properties;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Component.MsGraph.Models;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// Custom action to find meeting time that works for attendees using MS Graph
    /// </summary>
    [MsGraphCustomActionRegistration(FindMeetingTimes.FindMeetingTimesDeclarativeType)]
    public class FindMeetingTimes : BaseMsGraphCustomAction<List<CalendarSkillTimeSlotModel>>
    {
        /// <summary>
        /// Declarative type for the custom action
        /// </summary>
        public const string FindMeetingTimesDeclarativeType = "Microsoft.Graph.Calendar.FindMeetingTimes";

        /// <summary>
        /// Creates an instance of <seealso cref="FindMeetingTimes" />
        /// </summary>
        /// <param name="callerPath"></param>
        /// <param name="callerLine"></param>
        [JsonConstructor]
        public FindMeetingTimes([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        /// <summary>
        /// Gets or sets the list of attendees to the meeting
        /// </summary>
        /// <value>The list of attendees to the meeting</value>
        [JsonProperty("attendeesProperty")]
        public ObjectExpression<List<Attendee>> AttendeesProperty { get; set; }

        /// <summary>
        /// Gets or sets the duration of the meeting
        /// </summary>
        /// <value>The duration of th meeting</value>
        [JsonProperty("durationProperty")]
        public IntExpression DurationProperty { get; set; }

        /// <summary>
        /// Gets or sets the timezone in which to find meeting times for
        /// </summary>
        /// <value>The timezone for the search query</value>
        [JsonProperty("timeZoneProperty")]
        public StringExpression TimeZoneProperty { get; set; }

        public override string DeclarativeType => FindMeetingTimesDeclarativeType;

        protected override async Task<List<CalendarSkillTimeSlotModel>> CallGraphServiceWithResultAsync(GraphServiceClient client, DialogContext dc, CancellationToken cancellationToken)
        {
            var dcState = dc.State;
            var attendeesProperty = this.AttendeesProperty.GetValue(dcState);
            var duration = this.DurationProperty.GetValue(dcState);
            var timeZoneProperty = this.TimeZoneProperty.GetValue(dcState);
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneProperty);
            var attendees = attendeesProperty;

            MeetingTimeSuggestionsResult meetingTimesResult = await client.Me.FindMeetingTimes(attendees: attendees, minimumAttendeePercentage: 100, meetingDuration: new Duration(new TimeSpan(0, duration, 0)), maxCandidates: 10)
                                                                             .Request().PostAsync(cancellationToken);

            var results = new List<CalendarSkillTimeSlotModel>();
            foreach (var timeSlot in meetingTimesResult.MeetingTimeSuggestions)
            {
                if (timeSlot.Confidence >= 1)
                {
                    var start = DateTime.Parse(timeSlot.MeetingTimeSlot.Start.DateTime);
                    var end = DateTime.Parse(timeSlot.MeetingTimeSlot.End.DateTime);
                    results.Add(new CalendarSkillTimeSlotModel()
                    {
                        Start = TimeZoneInfo.ConvertTimeFromUtc(start, timeZone),
                        End = TimeZoneInfo.ConvertTimeFromUtc(end, timeZone),
                    });
                }
            }

            return results.OrderBy(s => s.Start).ToList();
        }
    }
}
