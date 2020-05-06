using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var events = EventsProperty.GetValue(dcState);

            var currentDateTime = DateTime.Now;
            var previousEvents = new List<object>();
            var upcomingEvents = new List<object>();

            foreach (var ev in events)
            {
                var start = DateTime.Parse(ev.Start.DateTime);
                var duration = DateTime.Parse(ev.End.DateTime).Subtract(DateTime.Parse(ev.Start.DateTime));

                if (start < currentDateTime)
                {
                    previousEvents.Add(new
                    {
                        ev.Subject,
                        ev.Start,
                        ev.End,
                        ev.Attendees,
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
                        Description = ev.BodyPreview,
                        Location = !string.IsNullOrEmpty(ev.Location.DisplayName) ? ev.Location.DisplayName : string.Empty,
                        DurationDays = duration.Days,
                        DurationHours = duration.Hours,
                        DurationMinutes = duration.Minutes
                    });
                }
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(SortEvents), new { previousEvents, upcomingEvents }, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.PreviousEventsProperty != null)
            {
                dcState.SetValue(PreviousEventsProperty, previousEvents);
            }

            if (this.UpcomingEventsProperty != null)
            {
                dcState.SetValue(UpcomingEventsProperty, upcomingEvents);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: new { previousEvents, upcomingEvents }, cancellationToken: cancellationToken);
        }
    }
}
