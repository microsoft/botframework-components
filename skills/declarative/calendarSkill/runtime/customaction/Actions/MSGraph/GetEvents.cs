using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.BotFramework.Composer.CustomAction;
using Microsoft.Graph;
using Microsoft.Graph.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Signifies that only future events should be included in the search results.
        /// </summary>
        [JsonProperty("isFutureProperty")]
        public BoolExpression IsFutureProperty { get; set; }

        [JsonProperty("timeZoneProperty")]
        public StringExpression TimeZoneProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var token = Token.GetValue(dcState);
            var startProperty = StartProperty.GetValue(dcState);
            var endProperty = EndProperty.GetValue(dcState);
            var timeZoneProperty = TimeZoneProperty.GetValue(dcState);
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneProperty);
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
            if (startProperty == null
                || startProperty.Value == DateTime.MinValue
                || (startProperty <= DateTime.UtcNow && isFuture))
            {
                startProperty = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
            }

            // if end date is not provided, default to end of the current day
            if (endProperty == null || endProperty.Value == DateTime.MinValue)
            {
                endProperty = startProperty.Value.Date.AddHours(23).AddMinutes(59);
            }

            var graphClient = MSGraphClient.GetAuthenticatedClient(token);

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
                    var startTZ = TimeZoneInfo.ConvertTimeFromUtc(ev.Start.ToDateTime(), timeZone);
                    var endTZ = TimeZoneInfo.ConvertTimeFromUtc(ev.End.ToDateTime(), timeZone);

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
                        var startTZ = TimeZoneInfo.ConvertTimeFromUtc(ev.Start.ToDateTime(), timeZone);
                        var endTZ = TimeZoneInfo.ConvertTimeFromUtc(ev.End.ToDateTime(), timeZone);

                        ev.Start = DateTimeTimeZone.FromDateTime(startTZ, timeZone);
                        ev.End = DateTimeTimeZone.FromDateTime(endTZ, timeZone);

                        results.Add(ev);
                    }
                }
            }

            // These filters will be completed for the Search Events work item and are not currently being used. Notes here are for future implementation.
            // Filter results by title
            //if (titleProperty != null)
            //{
            //    var title = titleProperty as string;
            //    results = results.Where(r => r.Subject.ToLower().Contains(title.ToLower())).ToList();
            //}

            //// Filter results by location
            //if (locationProperty != null)
            //{
            //    var location = locationProperty as string;
            //    results = results.Where(r => r.Location.DisplayName.ToLower().Contains(location.ToLower())).ToList();
            //}

            //// Filter results by attendees
            //if (attendeesProperty != null)
            //{
            //    // TODO: update to use contacts from graph rather than string matching
            //    var attendees = attendeesProperty as List<string>;
            //    results = results.Where(r => attendees.TrueForAll(p => r.Attendees.Any(a => a.EmailAddress.Name.ToLower().Contains(p.ToLower())))).ToList();
            //}

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(GetEvents), results, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dc.State.SetValue(this.ResultProperty.GetValue(dc.State), JToken.FromObject(results));
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: results, cancellationToken: cancellationToken);
        }
    }
}
