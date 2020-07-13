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
    public class SortEvents : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.SortEvents";

        [JsonConstructor]
        public SortEvents([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("eventsProperty")]
        public ArrayExpression<Event> EventsProperty { get; set; }

        [JsonProperty("previousEventsProperty")]
        public string PreviousEventsProperty { get; set; }

        [JsonProperty("upcomingEventsProperty")]
        public string UpcomingEventsProperty { get; set; }

        [JsonProperty("timeZoneProperty")]
        public StringExpression TimeZoneProperty { get; set; }

        [JsonProperty("eventDictionaryProperty")]
        public string EventDictionaryProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var events = EventsProperty.GetValue(dcState);
            var timeZoneProperty = this.TimeZoneProperty.GetValue(dcState);
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneProperty);
            var currentDateTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, timeZone);
            var eventsByDate = events
                .OrderBy(ev => DateTime.Parse(ev.Start.DateTime).Date)
                .GroupBy(ev => DateTime.Parse(ev.Start.DateTime).Date)
                .ToDictionary(g => g.Key, g => SortEventsForDay(g.ToList(), currentDateTime));

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(SortEvents), eventsByDate, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.EventDictionaryProperty != null)
            {
                dcState.SetValue(EventDictionaryProperty, eventsByDate);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: eventsByDate, cancellationToken: cancellationToken);
        }

        private object SortEventsForDay(List<Event> events, DateTime currentDateTime)
        {
            var previousEvents = new List<object>();
            var upcomingEvents = new List<object>();

            foreach (var ev in events)
            {
                var start = DateTime.Parse(ev.Start.DateTime);
                var duration = DateTime.Parse(ev.End.DateTime).Subtract(start);

                if (start < currentDateTime)
                {
                    previousEvents.Add(new
                    {
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
                        DurationMinutes = duration.Minutes
                    });
                }
                else
                {
                    upcomingEvents.Add(new
                    {
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
                        DurationMinutes = duration.Minutes
                    });
                }
            }

            return new { previousEvents, upcomingEvents };
        }
    }
}
