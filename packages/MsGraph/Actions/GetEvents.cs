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
    using Microsoft.Bot.Builder.AI.Luis;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Component.MsGraph.Models;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// This action gets events from the user's MS Outlook calendar.
    /// The CalendarView API is used (rather than the events API) because it gives the most consistent results.
    /// However, it does not allow for filtering by any other properties except start and end date.
    /// For that reason, additional filtering based on title, attendees, location, etc should happen in this
    /// action after the API call has been made.
    /// </summary>
    [MsGraphCustomActionRegistration(GetEvents.GetEventsDeclarativeType)]
    public class GetEvents : BaseMsGraphCustomAction<List<CalendarSkillEventModel>>
    {
        public const string GetEventsDeclarativeType = "Microsoft.Graph.Calendar.GetEvents";

        /// <summary>
        /// Creates an instance of <seealso cref="GetEvents" />
        /// </summary>
        /// <param name="callerPath"></param>
        /// <param name="callerLine"></param>
        [JsonConstructor]
        public GetEvents([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        /// <summary>
        /// Gets or sets the title of event to query
        /// </summary>
        /// <value>Title of the event to query</value>
        [JsonProperty("titleProperty")]
        public StringExpression TitleProperty { get; set; }

        /// <summary>
        /// Gets or sets the location of the event to query
        /// </summary>
        /// <value>The location of the event to query</value>
        [JsonProperty("locationProperty")]
        public StringExpression LocationProperty { get; set; }

        /// <summary>
        /// Gets or sets the attendees of the event to query
        /// </summary>
        /// <value>The attendees of the event to query</value>
        [JsonProperty("attendeesProperty")]
        public ArrayExpression<string> AttendeesProperty { get; set; }

        /// <summary>
        /// Gets or sets the start time of the event to query
        /// </summary>
        /// <value>The start time of the event to query</value>
        [JsonProperty("startProperty")]
        public ObjectExpression<DateTime?> StartProperty { get; set; }

        /// <summary>
        /// Gets or sets the end time of the event to query
        /// </summary>
        /// <value>The end time of the event to query</value>
        [JsonProperty("endProperty")]
        public ObjectExpression<DateTime?> EndProperty { get; set; }

        /// <summary>
        /// Gets or sets date time type of the event to query
        /// </summary>
        /// <value>The date time type of the event to query</value>
        [JsonProperty("dateTimeTypeProperty")]
        public StringExpression DateTimeTypeProperty { get; set; }

        /// <summary>
        /// Gets or sets the ordinal of the event to query
        /// </summary>
        /// <value>The ordinal of the event to query</value>
        [JsonProperty("ordinalProperty")]
        public ObjectExpression<OrdinalV2> OrdinalProperty { get; set; }

        /// <summary>
        /// Gets or sets the timezone of the event to query
        /// </summary>
        /// <value>The timezone of the event to query</value>
        [JsonProperty("timeZoneProperty")]
        public StringExpression TimeZoneProperty { get; set; }

        /// <summary>
        /// Gets or sets the user email address
        /// </summary>
        /// <value>The email address of the user</value>
        [JsonProperty("userEmailProperty")]
        public StringExpression UserEmailProperty { get; set; }

        /// <summary>
        /// Gets or sets whether to show only future events in the result set
        /// </summary>
        /// <value><c>True</c> if we want to show only future events. <c>False</c> if otherwise.</value>
        [JsonProperty("futureEventsOnlyProperty")]
        public BoolExpression FutureEventsOnlyProperty { get; set; }

        /// <summary>
        /// Gets or sets the max number of results to return from the query
        /// </summary>
        /// <value>The max number of results</value>
        [JsonProperty("maxResultsProperty")]
        public IntExpression MaxResultsProperty { get; set; }

        public override string DeclarativeType => GetEventsDeclarativeType;

        /// <summary>
        /// Calls the graph service to get a list of events based on the query
        /// </summary>
        /// <param name="client"></param>
        /// <param name="dc"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override async Task<List<CalendarSkillEventModel>> CallGraphServiceWithResultAsync(GraphServiceClient client, DialogContext dc, CancellationToken cancellationToken)
        {
            var dcState = dc.State;
            var startProperty = this.StartProperty.GetValue(dcState);
            var endProperty = this.EndProperty.GetValue(dcState);
            var timeZoneProperty = this.TimeZoneProperty.GetValue(dcState);
            var ordinalProperty = this.OrdinalProperty.GetValue(dcState);
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneProperty);
            var dateTimeTypeProperty = this.DateTimeTypeProperty.GetValue(dcState);
            var isFuture = this.FutureEventsOnlyProperty.GetValue(dcState);
            var maxResults = this.MaxResultsProperty.GetValue(dcState);
            var userEmail = this.UserEmailProperty.GetValue(dcState);
            var titleProperty = string.Empty;
            var locationProperty = string.Empty;
            var attendeesProperty = new List<string>();

            if (this.TitleProperty != null)
            {
                titleProperty = this.TitleProperty.GetValue(dcState);
            }

            if (this.LocationProperty != null)
            {
                locationProperty = this.LocationProperty.GetValue(dcState);
            }

            if (this.AttendeesProperty != null)
            {
                attendeesProperty = this.AttendeesProperty.GetValue(dcState);
            }

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

            IUserCalendarViewCollectionPage events = await client.Me.CalendarView.Request(queryOptions).GetAsync(cancellationToken);

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
                events = await events.NextPageRequest.GetAsync();
                if (events?.Count > 0)
                {
                    foreach (var ev in events)
                    {
                        parsedEvents.Add(this.ParseEvent(ev, timeZone, index++, userEmail));
                    }
                }
            }

            // These filters will be completed for the Search Events work item and are not currently being used. Notes here are for future implementation.
            // Filter results by datetime if dateTimeType is a specific datetime
            if (dateTimeTypeProperty != null && dateTimeTypeProperty.Contains("time"))
            {
                parsedEvents = parsedEvents.Where(r => DateTime.Parse(r.Start.DateTime) == startProperty).ToList();
            }

            // Filter results by title
            if (titleProperty != null)
            {
                var title = titleProperty;
                parsedEvents = parsedEvents.Where(r => r.Subject.ToLower().Contains(title.ToLower())).ToList();
            }

            // Filter results by location
            if (locationProperty != null)
            {
                var location = locationProperty;
                parsedEvents = parsedEvents.Where(r => r.Location.ToLower().Contains(location.ToLower())).ToList();
            }

            // Filter results by attendees
            if (attendeesProperty != null)
            {
                // TODO: update to use contacts from graph rather than string matching
                var attendees = attendeesProperty;
                parsedEvents = parsedEvents.Where(r => attendees.TrueForAll(p => r.Attendees.Any(a => a.EmailAddress.Name.ToLower().Contains(p.ToLower())))).ToList();
            }

            // Get result by order
            if (parsedEvents.Any() && ordinalProperty != null)
            {
                long offset = -1;
                if (ordinalProperty.RelativeTo == "start" || ordinalProperty.RelativeTo == "current")
                {
                    offset = ordinalProperty.Offset - 1;
                }
                else if (ordinalProperty.RelativeTo == "end")
                {
                    offset = parsedEvents.Count - ordinalProperty.Offset - 1;
                }

                if (offset >= 0 && offset < parsedEvents.Count)
                {
                    parsedEvents = new List<CalendarSkillEventModel>() { parsedEvents[(int)offset] };
                }
            }

            // TODO: Support for events that last all day or span multiple days. Needs design
            return parsedEvents
                .Where(ev => ev.IsAllDay == false && DateTime.Parse(ev.Start.DateTime).Date >= startProperty.Value.Date)
                .OrderBy(ev => DateTime.Parse(ev.Start.DateTime).Date)
                .Take(maxResults)
                .ToList();
        }

        /// <summary>
        /// Convers the graph's event model into our own internal <seealso cref="CalendarSkillEventModel" />
        /// </summary>
        /// <param name="ev"></param>
        /// <param name="timeZone"></param>
        /// <param name="index"></param>
        /// <param name="userEmail"></param>
        /// <returns></returns>
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
