﻿using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.BotFramework.Composer.CustomAction.Models;

namespace Microsoft.BotFramework.Composer.CustomAction.Actions.MSGraph
{
    /// <summary>
    /// This action gets events from the user's MS Outlook calendar. 
    /// The CalendarView API is used (rather than the events API) because it gives the most consistent results. 
    /// However, it does not allow for filtering by any other properties except start and end date. 
    /// For that reason, additional filtering based on title, attendees, location, etc should happen in this 
    /// action after the API call has been made.
    /// </summary>
    public class GetEvents : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.GetEvents";

        [JsonConstructor]
        public GetEvents([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public StringExpression ResultProperty { get; set; }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        [JsonProperty("titleProperty")]
        public StringExpression TitleProperty { get; set; }

        [JsonProperty("locationProperty")]
        public StringExpression LocationProperty { get; set; }

        [JsonProperty("attendeesProperty")]
        public ArrayExpression<string> AttendeesProperty { get; set; }

        [JsonProperty("startProperty")]
        public ObjectExpression<DateTime?> StartProperty { get; set; }

        [JsonProperty("endProperty")]
        public ObjectExpression<DateTime?> EndProperty { get; set; }

        [JsonProperty("dateTimeTypeProperty")]
        public StringExpression DateTimeTypeProperty { get; set; }

        [JsonProperty("ordinalProperty")]
        public ObjectExpression<OrdinalV2> OrdinalProperty { get; set; }

        [JsonProperty("timeZoneProperty")]
        public StringExpression TimeZoneProperty { get; set; }

        [JsonProperty("userEmailProperty")]
        public StringExpression UserEmailProperty { get; set; }

        [JsonProperty("futureEventsOnlyProperty")]
        public BoolExpression FutureEventsOnlyProperty { get; set; }

        [JsonProperty("maxResultsProperty")]
        public IntExpression MaxResultsProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var token = Token.GetValue(dcState);
            var startProperty = StartProperty.GetValue(dcState);
            var endProperty = EndProperty.GetValue(dcState);
            var timeZoneProperty = TimeZoneProperty.GetValue(dcState);
            var ordinalProperty = OrdinalProperty.GetValue(dcState);
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneProperty);
            var dateTimeTypeProperty = DateTimeTypeProperty.GetValue(dcState);
            var isFuture = FutureEventsOnlyProperty.GetValue(dcState);
            var maxResults = MaxResultsProperty.GetValue(dcState);
            var userEmail = UserEmailProperty.GetValue(dcState);
            var titleProperty = string.Empty;
            var locationProperty = string.Empty;
            var attendeesProperty = new List<string>();

            if (TitleProperty != null)
            {
                titleProperty = TitleProperty.GetValue(dcState);
            }

            if (LocationProperty != null)
            {
                locationProperty = LocationProperty.GetValue(dcState);
            }

            if (AttendeesProperty != null)
            {
                attendeesProperty = AttendeesProperty.GetValue(dcState);
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

            var httpClient = dc.Context.TurnState.Get<HttpClient>() ?? new HttpClient();
            var graphClient = MSGraphClient.GetAuthenticatedClient(token, httpClient);
            var parsedEvents = new List<CalendarSkillEventModel>();

            // Define the time span for the calendar view.
            var queryOptions = new List<QueryOption>
            {
                // The API expects the parameters in UTC, but the datetimes come into the action in the user's timezone
                new QueryOption("startDateTime", TimeZoneInfo.ConvertTimeToUtc(startProperty.Value, timeZone).ToString("o")),
                new QueryOption("endDateTime", TimeZoneInfo.ConvertTimeToUtc(endProperty.Value, timeZone).ToString("o")),
                new QueryOption("$orderBy", "start/dateTime"),
            };

            IUserCalendarViewCollectionPage events = null;
            try
            {
                events = await graphClient.Me.CalendarView.Request(queryOptions).GetAsync();
            }
            catch (ServiceException ex)
            {
                throw MSGraphClient.HandleGraphAPIException(ex);
            }

            int index = 0;
            if (events?.Count > 0)
            {
                foreach (var ev in events)
                {
                    parsedEvents.Add(ParseEvent(ev, timeZone, index++, userEmail));
                }
            }

            while (events.NextPageRequest != null)
            {
                events = await events.NextPageRequest.GetAsync();
                if (events?.Count > 0)
                {
                    foreach (var ev in events)
                    {
                        parsedEvents.Add(ParseEvent(ev, timeZone, index++, userEmail));
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
            parsedEvents = parsedEvents
                .Where(ev => ev.IsAllDay == false && DateTime.Parse(ev.Start.DateTime).Date >= startProperty.Value.Date)
                .OrderBy(ev => DateTime.Parse(ev.Start.DateTime).Date)
                .Take(maxResults)
                .ToList();

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(DeclarativeType, parsedEvents, valueType: DeclarativeType, label: DeclarativeType).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dc.State.SetValue(this.ResultProperty.GetValue(dc.State), JToken.FromObject(parsedEvents));
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: parsedEvents, cancellationToken: cancellationToken);
        }

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
