// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;

namespace Microsoft.Bot.Components.Telephony.Actions
{
    /// <summary>
    /// Aggregates input until it matches a regex pattern and then stores the result in an output property
    /// </summary>
    public class BatchDTMF : Dialog
    {
        /// <summary>
        /// Class identifier.
        /// </summary>
        [JsonProperty("$kind")]
        public const string Kind = "Microsoft.Telephony.BatchDTMF";
        private const string AggregationDialogMemory = "dialog.aggregation";

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchDTMF"/> class.
        /// </summary>
        /// <param name="sourceFilePath">Optional, source file full path.</param>
        /// <param name="sourceLineNumber">Optional, line number in source file.</param>
        [JsonConstructor]
        public BatchDTMF([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            : base()
        {
            // enable instances of this command as debug break point
            this.RegisterSourceLocation(sourceFilePath, sourceLineNumber);
        }

        /// <summary>
        /// Gets or sets the regex pattern to use to decide when the dialog has aggregated the whole message.
        /// </summary>
        [JsonProperty("terminationConditionRegexPattern")]
        public StringExpression TerminationConditionRegexPattern { get; set; }

        /// <summary>
        /// Gets or sets the property to assign the result to.
        /// </summary>
        [JsonProperty("property")]
        public StringExpression Property { get; set; }

        /// <inheritdoc/>
        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            //Get value of temrination string from expression
            string regexPattern = this.TerminationConditionRegexPattern?.GetValue(dc.State);

            if (dc.Context.Activity.Type != "message")
            {
                return new DialogTurnResult(DialogTurnStatus.Waiting);
            }

            //Is the current message the termination string?
            if (new Regex(regexPattern).IsMatch(dc.Context.Activity.Text))
            {
                //If so, store the message in the property specified and end this dialog
                //Get property from expression
                string property = this.Property?.GetValue(dc.State);

                dc.State.SetValue(property, dc.Context.Activity.Text);
                return await dc.EndDialogAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                //else, append the message to an aggregation memory state and end the turn
                dc.State.SetValue(AggregationDialogMemory, dc.Context.Activity.Text);
                return new DialogTurnResult(DialogTurnStatus.Waiting);
            }
        }

        /// <inheritdoc/>
        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default)
        {
            //Get value of temrination string from expression
            string regexPattern = this.TerminationConditionRegexPattern?.GetValue(dc.State);

            //append the message to the aggregation memory state
            var existingAggregation = dc.State.GetValue(AggregationDialogMemory, () => string.Empty);
            existingAggregation += dc.Context.Activity.Text;

            //Is the current aggregated message the termination string?
            if (new Regex(regexPattern).IsMatch(existingAggregation))
            {
                //If so, save it to the output property and end the dialog
                //Get property from expression
                string property = this.Property?.GetValue(dc.State);

                dc.State.SetValue(property, existingAggregation);
                return await dc.EndDialogAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                //else, save the updated aggregation and end the turn
                dc.State.SetValue(AggregationDialogMemory, existingAggregation);
                return new DialogTurnResult(DialogTurnStatus.Waiting);
            }
        }
    }
}