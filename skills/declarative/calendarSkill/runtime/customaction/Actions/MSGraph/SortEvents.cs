using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.BotFramework.Composer.CustomAction.Models;
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

        [JsonProperty("groupByDateProperty")]
        public BoolExpression GroupByDateProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var events = InputProperty.GetValue(dcState);
            var timeZoneProperty = this.TimeZoneProperty.GetValue(dcState);
            var groupByDateProperty = GroupByDateProperty.GetValue(dcState);
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneProperty);
            var currentDateTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, timeZone);
            object result = null;

            if (groupByDateProperty)
            {
                result = events
                    .Where(ev => ev.IsAllDay == false)
                    .OrderBy(ev => DateTime.Parse(ev.Start.DateTime).Date)
                    .GroupBy(ev => DateTime.Parse(ev.Start.DateTime).Date)
                    .Select(g => new { date = g.Key, events = ParseEvents(currentDateTime, g.ToList()) })
                    .ToList();
            }
            else
            {
                var sortedEvents = events
                    .Where(ev => ev.IsAllDay == false)
                    .OrderBy(ev => DateTime.Parse(ev.Start.DateTime).Date);
                result = ParseEvents(currentDateTime, events);
            }


            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(SortEvents), result, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, JToken.FromObject(result));
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: result, cancellationToken: cancellationToken);
        }

        private List<object> ParseEvents(DateTime currentDateTime, List<Event> events)
        {
            var eventsList = new List<object>();
            var i = 0;

            foreach (var ev in events)
            {
                eventsList.Add(new CalendarSkillEventModel(ev, currentDateTime, i));
            }

            return eventsList;
        }
    }
}
