// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Components.Graph.Actions
{
    using System;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveExpressions.Properties;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.TraceExtensions;
    using Microsoft.Recognizers.Text.DateTime;
    using Newtonsoft.Json;

    /// <summary>
    /// This action calls the Microsoft.Recognizers.Text library for recognizing DateTimes from strings.
    /// This has proven to be more consistent that LUIS datetime recognition by allowing the use of the
    /// user's current timezone time as a relative datetime rather than a mix of absolute times and UTC times.
    /// </summary>
    [GraphCustomActionRegistration(RecognizeDateTime.DeclarativeType)]
    public class RecognizeDateTime : Dialog
    {
        /// <summary>
        /// The declarative type name for this action.
        /// </summary>
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.Helpers.RecognizeDateTime";

        /// <summary>
        /// Initializes a new instance of the <see cref="RecognizeDateTime"/> class.
        /// </summary>
        /// <param name="callerPath">The path of the caller.</param>
        /// <param name="callerLine">The line number at which the method is called.</param>
        [JsonConstructor]
        public RecognizeDateTime([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        /// <summary>
        /// Gets or sets the property name where the result of the action should be stored.
        /// </summary>
        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        /// <summary>
        /// Gets or sets the query to perform recognition on.
        /// </summary>
        [JsonProperty("query")]
        public StringExpression Query { get; set; }

        /// <summary>
        /// Gets or sets the time zone for converting recognized datetimes.
        /// </summary>
        [JsonProperty("timeZone")]
        public StringExpression TimeZone { get; set; }

        /// <inheritdoc/>
        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var queryProperty = this.Query.GetValue(dcState);
            var timeZoneProperty = this.TimeZone.GetValue(dcState);
            var culture = this.GetCulture(dc);
            var timeZoneNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GraphUtils.ConvertTimeZoneFormat(timeZoneProperty));

            var results = DateTimeRecognizer.RecognizeDateTime(queryProperty, culture, DateTimeOptions.CalendarMode, refTime: timeZoneNow);

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(RecognizeDateTime), results, valueType: DeclarativeType, label: DeclarativeType).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(this.ResultProperty, results);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: results, cancellationToken: cancellationToken).ConfigureAwait(false);
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
