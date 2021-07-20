// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Components.Telephony.Actions
{
    /// <summary>
    /// Aggregates input until it matches a regex pattern and then stores the result in an output property.
    /// </summary>
    public class RegexAggregatorInput : Dialog
    {
        /// <summary>
        /// Class identifier.
        /// </summary>
        [JsonProperty("$kind")]
        public const string Kind = "Microsoft.Telephony.RegexAggregatorInput";
        private const string AggregationDialogMemory = "dialog.aggregation";

        /// <summary>
        /// Initializes a new instance of the <see cref="RegexAggregatorInput"/> class.
        /// </summary>
        /// <param name="sourceFilePath">Optional, source file full path.</param>
        /// <param name="sourceLineNumber">Optional, line number in source file.</param>
        [JsonConstructor]
        public RegexAggregatorInput([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
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

        /// <summary>
        /// Gets or sets the activity to send to the user.
        /// </summary>
        /// <value>
        /// An activity template.
        /// </value>
        [JsonProperty("prompt")]
        public ITemplate<Activity> Prompt { get; set; }

        /// <inheritdoc/>
        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            ITemplate<Activity> template = this.Prompt ?? throw new InvalidOperationException($"InputDialog is missing Prompt.");
            IMessageActivity msg = await this.Prompt.BindAsync(dc, cancellationToken: cancellationToken).ConfigureAwait(false);

            if (msg != null && string.IsNullOrEmpty(msg.InputHint))
            {
                msg.InputHint = InputHints.ExpectingInput;
            }

            var properties = new Dictionary<string, string>()
                {
                    { "template", JsonConvert.SerializeObject(template) },
                    { "result", msg == null ? string.Empty : JsonConvert.SerializeObject(msg, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }) },
                };
            TelemetryClient.TrackEvent("GeneratorResult", properties);

            await dc.Context.SendActivityAsync(msg, cancellationToken).ConfigureAwait(false);

            return new DialogTurnResult(DialogTurnStatus.Waiting);
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
            if (Regex.Match(existingAggregation, regexPattern).Success)
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