using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Microsoft.Recognizers.Text.DateTime;
using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.BotFramework.Composer.CustomAction.Models;

namespace Microsoft.BotFramework.Composer.CustomAction.Actions.MSGraph
{
    /// <summary>
    /// This action calls the Microsoft.Recognizers.Text library for recognizing DateTimes from strings. 
    /// This has proven to be more consistent that LUIS datetime recognition by allowing the use of the 
    /// user's current timezone time as a relative datetime rather than a mix of absolute times and UTC times.
    /// </summary>
    public class CheckAvailability : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.CheckAvailability";

        [JsonConstructor]
        public CheckAvailability([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("inputProperty")]
        public ObjectExpression<List<CalendarSkillEventModel>> InputProperty { get; set; }

        [JsonProperty("startProperty")]
        public ObjectExpression<DateTime?> StartProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var events = InputProperty.GetValue(dcState);
            var startProperty = StartProperty.GetValue(dcState);
            var culture = GetCulture(dc);
            
            List<CalendarSkillEventModel> conflictEvents = new List<CalendarSkillEventModel>();
            CalendarSkillEventModel previousEvent = null;
            CalendarSkillEventModel nextEvent = null;
            if (events != null)
            {
                foreach (var ev in events)
                {
                    var start = DateTime.Parse(ev.Start.DateTime);
                    var end = DateTime.Parse(ev.End.DateTime);
                    if (start <= startProperty && startProperty < end)
                    {
                        conflictEvents.Add(ev);
                    }
                    else if (end <= startProperty && (previousEvent == null || DateTime.Parse(previousEvent.End.DateTime) < end))
                    {
                        previousEvent = ev;
                    }
                    else if (start > startProperty)
                    {
                        nextEvent = ev;
                        break;
                    }
                }
            }

            var results = new
            {
                previousEvent,
                nextEvent,
                conflictEvents
            };

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(CheckAvailability), results, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, results);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: results, cancellationToken: cancellationToken);
        }

        private string GetCulture(DialogContext dc)
        {
            if (!string.IsNullOrEmpty(dc.Context.Activity.Locale))
            {
                return dc.Context.Activity.Locale;
            }

            return Microsoft.Recognizers.Text.Culture.English;
        }
    }
}
