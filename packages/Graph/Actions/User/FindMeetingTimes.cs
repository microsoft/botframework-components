// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.Graph.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
    /// Custom action to find meeting time that works for attendees using MS Graph.
    /// </summary>
    [GraphCustomActionRegistration(FindMeetingTimes.FindMeetingTimesDeclarativeType)]
    public class FindMeetingTimes : BaseMsGraphCustomAction<List<CalendarSkillTimeSlotModel>>
    {
        /// <summary>
        /// Declarative type for the custom action.
        /// </summary>
        private const string FindMeetingTimesDeclarativeType = "Microsoft.Graph.Calendar.FindMeetingTimes";

        /// <summary>
        /// Initializes a new instance of the <see cref="FindMeetingTimes"/> class.
        /// </summary>
        /// <param name="callerPath">Caller path.</param>
        /// <param name="callerLine">Caller line.</param>
        [JsonConstructor]
        public FindMeetingTimes([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        /// <summary>
        /// Gets or sets the list of attendees to the meeting.
        /// </summary>
        /// <value>The list of attendees to the meeting.</value>
        [JsonProperty("attendees")]
        public ArrayExpression<Attendee> Attendees { get; set; }

        /// <summary>
        /// Gets or sets the duration of the meeting.
        /// </summary>
        /// <value>The duration of th meeting.</value>
        [JsonProperty("duration")]
        public IntExpression Duration { get; set; }

        /// <summary>
        /// Gets or sets the timezone in which to find meeting times for.
        /// </summary>
        /// <value>The timezone for the search query.</value>
        [JsonProperty("timeZone")]
        public StringExpression TimeZone { get; set; }

        /// <inheritdoc />
        public override string DeclarativeType => FindMeetingTimesDeclarativeType;

        /// <inheritdoc />
        internal override async Task<List<CalendarSkillTimeSlotModel>> CallGraphServiceWithResultAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            var attendees = (List<Attendee>)parameters["Attendees"];
            var duration = (int)parameters["Duration"];
            var timeZone = GraphUtils.ConvertTimeZoneFormat((string)parameters["Timezone"]);

            MeetingTimeSuggestionsResult meetingTimesResult = await client.Me
                .FindMeetingTimes(attendees: attendees, minimumAttendeePercentage: 100, meetingDuration: new Duration(new TimeSpan(0, duration, 0)), maxCandidates: 10)
                .Request()
                .PostAsync(cancellationToken)
                .ConfigureAwait(false);

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

        /// <inheritdoc />
        protected override void PopulateParameters(DialogStateManager state, Dictionary<string, object> parameters)
        {
            parameters.Add("Attendees", this.Attendees.GetValue(state));
            parameters.Add("Duration", this.Duration.GetValue(state));
            parameters.Add("Timezone", this.TimeZone.GetValue(state));
        }
    }
}
