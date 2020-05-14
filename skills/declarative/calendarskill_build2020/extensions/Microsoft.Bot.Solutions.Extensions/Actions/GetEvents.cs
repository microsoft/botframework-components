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

        [JsonProperty("startProperty")]
        public ObjectExpression<DateTime?> StartProperty { get; set; }

        [JsonProperty("endProperty")]
        public ObjectExpression<DateTime?> EndProperty { get; set; }

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

            if (startProperty == null || startProperty.Value == DateTime.MinValue)
            {
                startProperty = DateTime.UtcNow.Date;
            }
            else
            {
                startProperty = startProperty.Value.Date.ToUniversalTime();
            }

            if (endProperty == null || endProperty.Value == DateTime.MinValue)
            {
                endProperty = startProperty.Value.Date.AddHours(23).AddMinutes(59);
            }

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
