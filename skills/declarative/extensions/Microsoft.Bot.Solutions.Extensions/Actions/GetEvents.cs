using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Graph;
using Microsoft.Graph.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Solutions.Extensions.Actions
{
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
        public string ResultProperty { get; set; }

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
            if (startProperty == null || startProperty.Value == DateTime.MinValue || isFuture)
            {
                startProperty = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone).Date;
            }
            //else
            //{
            //    if (startProperty.Value.TimeOfDay != new TimeSpan(0, 0, 0))
            //    {
            //        // if that start property is the beginning of the day, don't convert from UTC
            //        startProperty = TimeZoneInfo.ConvertTimeFromUtc(startProperty.Value, timeZone);
            //    }
            //}

            // if end date is not provided, default to start property +7days
            if (endProperty == null || endProperty.Value == DateTime.MinValue)
            {
                endProperty = startProperty.Value.Date.AddHours(23).AddMinutes(59);
            }
            //else
            //{
            //    endProperty = TimeZoneInfo.ConvertTimeFromUtc(endProperty.Value, timeZone);
            //}

            var graphClient = GraphClient.GetAuthenticatedClient(token);

            var results = new List<Event>();

            // Define the time span for the calendar view.
            var queryOptions = new List<QueryOption>
            {
                new QueryOption("startDateTime", startProperty.Value.ToString("o")),
                new QueryOption("endDateTime", endProperty.Value.ToString("o")),
                new QueryOption("$orderBy", "start/dateTime"),
            };

            IUserCalendarViewCollectionPage events = null;
            try
            {
                events = await graphClient.Me.CalendarView.Request(queryOptions).GetAsync();
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
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

            // Filter results by title
            if (titleProperty != null)
            {
                var title = titleProperty as string;
                results = results.Where(r => r.Subject.ToLower().Contains(title.ToLower())).ToList();
            }

            // Filter results by location
            if (locationProperty != null)
            {
                var location = locationProperty as string;
                results = results.Where(r => r.Location.DisplayName.ToLower().Contains(location.ToLower())).ToList();
            }

            // Filter results by attendees
            if (attendeesProperty != null)
            {
                // TODO: update to use contacts from graph rather than string matching
                var attendees = attendeesProperty as List<string>;
                results = results.Where(r => attendees.TrueForAll(p => r.Attendees.Any(a => a.EmailAddress.Name.ToLower().Contains(p.ToLower())))).ToList();
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(GetEvents), results, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, results);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: results, cancellationToken: cancellationToken);
        }
    }
}
