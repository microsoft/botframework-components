// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Components.Graph;
using Microsoft.Recognizers.Text.DateTime;
using Newtonsoft.Json;

namespace Microsoft.Bot.Components.Calendar.Actions
{
    /// <summary>
    /// This action calls the Microsoft.Recognizers.Text library for recognizing DateTimes from strings. 
    /// This has proven to be more consistent that LUIS datetime recognition by allowing the use of the 
    /// user's current timezone time as a relative datetime rather than a mix of absolute times and UTC times.
    /// </summary>
    public class RecognizeDateTime : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Bot.Solutions.RecognizeDateTime";

        [JsonConstructor]
        public RecognizeDateTime([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("queryProperty")]
        public StringExpression QueryProperty { get; set; }

        [JsonProperty("timeZoneProperty")]
        public StringExpression TimeZoneProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var queryProperty = QueryProperty.GetValue(dcState);
            var timeZoneProperty = TimeZoneProperty.GetValue(dcState);
            var culture = GetCulture(dc);
            var timeZoneNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GraphUtils.ConvertTimeZoneFormat(timeZoneProperty));

            var results = DateTimeRecognizer.RecognizeDateTime(queryProperty, culture, refTime: timeZoneNow);

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(RecognizeDateTime), results, valueType: DeclarativeType, label: DeclarativeType).ConfigureAwait(false);

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
