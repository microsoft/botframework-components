using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
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

        /// <summary>
        /// Signifies that only future events should be included in the search results.
        /// </summary>
        [JsonProperty("isFutureProperty")]
        public BoolExpression IsFutureProperty { get; set; }

        [JsonProperty("orderProperty")]
        public StringExpression OrderProperty { get; set; }

        [JsonProperty("timeZoneProperty")]
        public StringExpression TimeZoneProperty { get; set; }
        

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var token = Token.GetValue(dcState);
            var startProperty = StartProperty.GetValue(dcState);
            var endProperty = EndProperty.GetValue(dcState);
            var timeZoneProperty = TimeZoneProperty.GetValue(dcState);
            var orderProperty = OrderProperty.GetValue(dcState);
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneProperty);
            var dateTimeTypeProperty = DateTimeTypeProperty.GetValue(dcState);
            var isFuture = IsFutureProperty.GetValue(dcState);
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
            var results = new List<Event>();

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

            if (events?.Count > 0)
            {
                foreach (var ev in events)
                {
                    var startTZ = TimeZoneInfo.ConvertTimeFromUtc(DateTime.Parse(ev.Start.DateTime), timeZone);
                    var endTZ = TimeZoneInfo.ConvertTimeFromUtc(DateTime.Parse(ev.End.DateTime), timeZone);

                    ev.Start = DateTimeTimeZone.FromDateTime(startTZ, timeZone);
                    ev.End = DateTimeTimeZone.FromDateTime(endTZ, timeZone);

                    results.Add(ev);
                }
            }

            while (events.NextPageRequest != null)
            {
                events = await events.NextPageRequest.GetAsync();
                if (events?.Count > 0)
                {
                    foreach (var ev in events)
                    {
                        var startTZ = TimeZoneInfo.ConvertTimeFromUtc(DateTime.Parse(ev.Start.DateTime), timeZone);
                        var endTZ = TimeZoneInfo.ConvertTimeFromUtc(DateTime.Parse(ev.End.DateTime), timeZone);

                        ev.Start = DateTimeTimeZone.FromDateTime(startTZ, timeZone);
                        ev.End = DateTimeTimeZone.FromDateTime(endTZ, timeZone);

                        results.Add(ev);
                    }
                }
            }

            // These filters will be completed for the Search Events work item and are not currently being used. Notes here are for future implementation.
            // Filter results by datetime if dateTimeType is a specific datetime
            if (dateTimeTypeProperty != null && dateTimeTypeProperty.Contains("time"))
            {
                results = results.Where(r => DateTime.Parse(r.Start.DateTime) == startProperty).ToList();
            }

            // Filter results by title
            if (titleProperty != null)
            {
                var title = titleProperty;
                results = results.Where(r => r.Subject.ToLower().Contains(title.ToLower())).ToList();
            }

            //// Filter results by location
            if (locationProperty != null)
            {
                var location = locationProperty;
                results = results.Where(r => r.Location.DisplayName.ToLower().Contains(location.ToLower())).ToList();
            }

            //// Filter results by attendees
            if (attendeesProperty != null)
            {
                // TODO: update to use contacts from graph rather than string matching
                var attendees = attendeesProperty;
                results = results.Where(r => attendees.TrueForAll(p => r.Attendees.Any(a => a.EmailAddress.Name.ToLower().Contains(p.ToLower())))).ToList();
            }

            //// Get result by order
            if (results.Any() && orderProperty == "next")
            {
                // TODO: extend only 'next' to more general format
                results = new List<Event>() { results.First() };
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(GetEvents), results, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dc.State.SetValue(this.ResultProperty.GetValue(dc.State), JToken.FromObject(results));
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: results, cancellationToken: cancellationToken);
        }

        private List<object> ParseEvents(DateTime currentDateTime, List<Event> events)
        {
            var eventsList = new List<object>();
            var i = 0;

            foreach (var ev in events)
            {
                var start = DateTime.Parse(ev.Start.DateTime);
                var end = DateTime.Parse(ev.End.DateTime);
                var duration = end.Subtract(start);
                var isCurrentEvent = false;

                if (start <= currentDateTime && currentDateTime <= end
                    || start.AddMinutes(-30) <= currentDateTime && currentDateTime <= start)
                {
                    // If event is currently ongoing, or will start in the next 30 minutes
                    isCurrentEvent = true;
                }

                eventsList.Add(new
                {
                    Id = i++,
                    ev.Subject,
                    ev.Start,
                    ev.End,
                    ev.Attendees,
                    ev.IsOnlineMeeting,
                    ev.OnlineMeeting,
                    Description = ev.BodyPreview,
                    Location = !string.IsNullOrEmpty(ev.Location.DisplayName) ? ev.Location.DisplayName : string.Empty,
                    DurationDays = duration.Days,
                    DurationHours = duration.Hours,
                    DurationMinutes = duration.Minutes,
                    isRecurring = ev.Type == EventType.Occurrence || ev.Type == EventType.SeriesMaster ? true : false,
                    isCurrentEvent,
                    ev.IsOrganizer,
                    ev.ResponseStatus.Response,
                    ev.Organizer
                });
            }

            return eventsList;
        }
    }
}
