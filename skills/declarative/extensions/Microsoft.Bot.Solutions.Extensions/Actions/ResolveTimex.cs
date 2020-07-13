using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Microsoft.Recognizers.Text.DateTime;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Solutions.Extensions.Actions
{
    public class ResolveTimex : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Bot.Solutions.ResolveTimex";

        [JsonConstructor]
        public ResolveTimex([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("timexProperty")]
        public ArrayExpression<string> TimexProperty { get; set; }

        [JsonProperty("timeZoneProperty")]
        public StringExpression TimeZoneProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var timexProperty = TimexProperty.GetValue(dcState);
            var timeZoneProperty = TimeZoneProperty.GetValue(dcState);
            var culture = GetCulture(dc);
            var results = TimexResolver.Resolve(timexProperty.ToArray()).Values;

            // if type == date, do special timezone stuff

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(ResolveTimex), results, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            // if timex type == date
            // set start and end to full day (unconverted)

            // if timex == datetime
            // convert to timezone, set value

            // if timex == datetimerange
            // convert to timezone set start/end

            // if timex == time
            // set value



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
