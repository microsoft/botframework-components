using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
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
    /// This action sorts a list of MS Graph Event objects into a list of objects in the format {date, list<events>}
    /// This simplifies rendering the results in an Adaptive Card. 
    /// </summary>
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

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("inputProperty")]
        public ObjectExpression<List<Event>> InputProperty { get; set; }

        [JsonProperty("timeZoneProperty")]
        public StringExpression TimeZoneProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var events = InputProperty.GetValue(dcState);
            var timeZoneProperty = this.TimeZoneProperty.GetValue(dcState);
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneProperty);
            var currentDateTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, timeZone);
            var eventsByDate = events
                .Where(ev => ev.IsAllDay == false)
                .OrderBy(ev => DateTime.Parse(ev.Start.DateTime).Date)
                .GroupBy(ev => DateTime.Parse(ev.Start.DateTime).Date)
                .Select(g => new { date = g.Key, events = ParseEvents(currentDateTime, g.ToList()) })
                .ToList();

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(SortEvents), eventsByDate, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, JToken.FromObject(eventsByDate));
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: eventsByDate, cancellationToken: cancellationToken);
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

                if(start <= currentDateTime && currentDateTime <= end
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
