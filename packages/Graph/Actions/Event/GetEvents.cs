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
    /// This action gets events from the user's MS Outlook calendar.
    /// The CalendarView API is used (rather than the events API) because it gives the most consistent results.
    /// However, it does not allow for filtering by any other properties except start and end date.
    /// For that reason, additional filtering based on title, attendees, location, etc should happen in this
    /// action after the API call has been made.
    /// </summary>
    [GraphCustomActionRegistration(GetEvents.GetEventsDeclarativeType)]
    public class GetEvents : BaseMsGraphCustomAction<List<CalendarSkillEventModel>>
    {
        private const string GetEventsDeclarativeType = "Microsoft.Graph.Calendar.GetEvents";

        /// <summary>
        /// Initializes a new instance of the <see cref="GetEvents"/> class.
        /// </summary>
        /// <param name="callerPath">Caller path.</param>
        /// <param name="callerLine">Caller line.</param>
        [JsonConstructor]
        public GetEvents([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        /// <summary>
        /// Gets or sets the start time of the event to query.
        /// </summary>
        /// <value>The start time of the event to query.</value>
        [JsonProperty("start")]
        public ObjectExpression<DateTime?> Start { get; set; }

        /// <summary>
        /// Gets or sets the end time of the event to query.
        /// </summary>
        /// <value>The end time of the event to query.</value>
        [JsonProperty("end")]
        public ObjectExpression<DateTime?> End { get; set; }

        /// <summary>
        /// Gets or sets date time type of the event to query.
        /// </summary>
        /// <value>The date time type of the event to query.</value>
        [JsonProperty("dateTimeType")]
        public StringExpression DateTimeType { get; set; }

        /// <summary>
        /// Gets or sets the timezone of the event to query.
        /// </summary>
        /// <value>The timezone of the event to query.</value>
        [JsonProperty("timeZone")]
        public StringExpression TimeZone { get; set; }

        /// <summary>
        /// Gets or sets the user email address.
        /// </summary>
        /// <value>The email address of the user.</value>
        [JsonProperty("userEmail")]
        public StringExpression UserEmail { get; set; }

        /// <summary>
        /// Gets or sets whether to show only future events in the result set.
        /// </summary>
        /// <value><c>True</c> if we want to show only future events. <c>False</c> if otherwise.</value>
        [JsonProperty("futureEventsOnly")]
        public BoolExpression FutureEventsOnly { get; set; }

        /// <summary>
        /// Gets or sets the max number of results to return from the query.
        /// </summary>
        /// <value>The max number of results.</value>
        [JsonProperty("maxResults")]
        public IntExpression MaxResults { get; set; }

        /// <inheritdoc/>
        public override string DeclarativeType => GetEventsDeclarativeType;

        /// <inheritdoc/>
        internal override async Task<List<CalendarSkillEventModel>> CallGraphServiceWithResultAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            var startProperty = (DateTime?)parameters["StartProperty"];
            var endProperty = (DateTime?)parameters["EndProperty"];
            var timeZoneProperty = (string)parameters["TimeZoneProperty"];
            var dateTimeTypeProperty = (string)parameters["DateTimeTypeProperty"];
            var isFuture = (bool)parameters["FutureEventsOnlyProperty"];
            var maxResults = (int)parameters["MaxResultsProperty"];
            var userEmail = (string)parameters["UserEmailProperty"];
            var timeZone = GraphUtils.ConvertTimeZoneFormat((string)parameters["TimezoneProperty"]);

            // if start date is not provided, default to DateTime.Now
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

            // if datetime field just contains time but no date, use date today at use's timezone
            if (!dateTimeTypeProperty.Contains("date"))
            {
                if (startProperty != null)
                {
                    startProperty = now.Date + startProperty.Value.TimeOfDay;
                }

                if (endProperty != null)
                {
                    endProperty = now.Date + endProperty.Value.TimeOfDay;
                }
            }

            if (startProperty == null
                || startProperty.Value == DateTime.MinValue
                || (startProperty <= now && isFuture))
            {
                startProperty = now;
            }

            // if end date is not provided, default to end of the current day
            if (endProperty == null || endProperty.Value == DateTime.MinValue)
            {
                endProperty = startProperty.Value.Date.AddHours(23).AddMinutes(59);
            }

            var parsedEvents = new List<CalendarSkillEventModel>();

            // Define the time span for the calendar view.
            var queryOptions = new List<QueryOption>
            {
                // The API expects the parameters in UTC, but the datetimes come into the action in the user's timezone
                new QueryOption("startDateTime", TimeZoneInfo.ConvertTimeToUtc(startProperty.Value, timeZone).ToString("o")),
                new QueryOption("endDateTime", TimeZoneInfo.ConvertTimeToUtc(endProperty.Value, timeZone).ToString("o")),
                new QueryOption("$orderBy", "start/dateTime"),
            };

            IUserCalendarViewCollectionPage events = await client.Me.CalendarView.Request(queryOptions).GetAsync(cancellationToken).ConfigureAwait(false);

            int index = 0;
            if (events?.Count > 0)
            {
                foreach (var ev in events)
                {
                    parsedEvents.Add(this.ParseEvent(ev, timeZone, index++, userEmail));
                }
            }

            while (events.NextPageRequest != null)
            {
                events = await events.NextPageRequest.GetAsync().ConfigureAwait(false);
                if (events?.Count > 0)
                {
                    foreach (var ev in events)
                    {
                        parsedEvents.Add(this.ParseEvent(ev, timeZone, index++, userEmail));
                    }
                }
            }

            // Filter results by datetime if dateTimeType is a specific datetime
            if (dateTimeTypeProperty != null && dateTimeTypeProperty.Contains("time"))
            {
                parsedEvents = parsedEvents.Where(r => DateTime.Parse(r.Start.DateTime) == startProperty).ToList();
            }

            parsedEvents = parsedEvents
                .Where(ev => ev.IsAllDay == false && DateTime.Parse(ev.Start.DateTime).Date >= startProperty.Value.Date)
                .OrderBy(ev => DateTime.Parse(ev.Start.DateTime).Date)
                .Take(maxResults)
                .ToList();

            return parsedEvents;
        }

        /// <inheritdoc/>
        protected override void PopulateParameters(DialogStateManager state, Dictionary<string, object> parameters)
        {
            parameters.Add("StartProperty", this.Start.GetValue(state));
            parameters.Add("EndProperty", this.End.GetValue(state));
            parameters.Add("TimezoneProperty", this.TimeZone.GetValue(state));
            parameters.Add("DateTimeTypeProperty", this.DateTimeType.GetValue(state));
            parameters.Add("FutureEventsOnlyProperty", this.FutureEventsOnly.GetValue(state));
            parameters.Add("MaxResultsProperty", this.MaxResults.GetValue(state));
            parameters.Add("UserEmailProperty", this.UserEmail.GetValue(state));
        }

        /// <summary>
        /// Convers the graph's event model into our own internal <seealso cref="CalendarSkillEventModel" />.
        /// </summary>
        /// <param name="ev">Event.</param>
        /// <param name="timeZone">Timezone.</param>
        /// <param name="index">Index.</param>
        /// <param name="userEmail">User email.</param>
        /// <returns>Model representing the event.</returns>
        private CalendarSkillEventModel ParseEvent(Event ev, TimeZoneInfo timeZone, int index, string userEmail)
        {
            var currentDateTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, timeZone);
            var startTZ = TimeZoneInfo.ConvertTimeFromUtc(DateTime.Parse(ev.Start.DateTime), timeZone);
            var endTZ = TimeZoneInfo.ConvertTimeFromUtc(DateTime.Parse(ev.End.DateTime), timeZone);

            ev.Start = DateTimeTimeZone.FromDateTime(startTZ, timeZone);
            ev.End = DateTimeTimeZone.FromDateTime(endTZ, timeZone);

            return new CalendarSkillEventModel(ev, currentDateTime, index, userEmail);
        }
    }
}
