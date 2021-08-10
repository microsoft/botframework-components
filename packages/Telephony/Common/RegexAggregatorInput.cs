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
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Components.Telephony.Actions
{
    /// <summary>
    /// Aggregates input until it matches a regex pattern and then stores the result in an output property.
    /// </summary>
    public abstract class RegexAggregatorInput : Dialog
    {
        protected const string AggregationDialogMemory = "this.aggregation";

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
        /// Gets or sets interruption policy. 
        /// </summary>
        /// <value>
        /// Bool or expression which evalutes to bool.
        /// </value>
        [JsonProperty("allowInterruptions")]
        public BoolExpression AllowInterruptions { get; set; } = false;

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

        /// <summary>
        /// Gets or sets a value indicating whether the input should always prompt the user regardless of there being a value or not.
        /// </summary>
        /// <value>
        /// Bool or expression which evaluates to bool.
        /// </value>
        [JsonProperty("alwaysPrompt")]
        public BoolExpression AlwaysPrompt { get; set; }

        /// <summary>
        /// Gets or sets a regex describing input that should be flagged as handled and not bubble.
        /// </summary>
        /// <value>
        /// String or expression which evaluates to string.
        /// </value>
        [JsonProperty("interruptionMask")]
        public StringExpression InterruptionMask { get; set; }

        /// <inheritdoc/>
        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await PromptUserAsync(dc, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default)
        {
            //Check if we were interrupted. If we were, follow our logic for when we get interrupted.
            bool wasInterrupted = dc.State.GetValue(TurnPath.Interrupted, () => false);
            if (wasInterrupted)
            {
                return await PromptUserAsync(dc, cancellationToken).ConfigureAwait(false);
            }

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

        /// <inheritdoc/>
        protected override async Task<bool> OnPreBubbleEventAsync(DialogContext dc, DialogEvent e, CancellationToken cancellationToken)
        {
            if (e.Name == DialogEvents.ActivityReceived && dc.Context.Activity.Type == ActivityTypes.Message)
            {
                //Get interruption mask pattern from expression
                string regexPattern = this.InterruptionMask?.GetValue(dc.State);

                // Return true( already handled ) if input matches our regex interruption mask
                if (!string.IsNullOrEmpty(regexPattern) && Regex.Match(dc.Context.Activity.Text, regexPattern).Success)
                {
                    return true;
                }

                // Ask parent to perform recognition
                await dc.Parent.EmitEventAsync(AdaptiveEvents.RecognizeUtterance, value: dc.Context.Activity, bubble: false, cancellationToken: cancellationToken).ConfigureAwait(false);

                // Should we allow interruptions
                var canInterrupt = true;
                if (this.AllowInterruptions != null)
                {
                    var (allowInterruptions, error) = this.AllowInterruptions.TryGetValue(dc.State);
                    canInterrupt = error == null && allowInterruptions;
                }

                // Stop bubbling if interruptions ar NOT allowed
                return !canInterrupt;
            }

            return false;
        }

        private async Task<DialogTurnResult> PromptUserAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            //Do we already have a value stored? This would happen in the interruption case, a case in which we are looping over ourselves, or maybe we had a fatal error and had to restart the dialog tree
            var existingAggregation = dc.State.GetValue(AggregationDialogMemory, () => string.Empty);
            if (!string.IsNullOrEmpty(existingAggregation))
            {
                var alwaysPrompt = this.AlwaysPrompt?.GetValue(dc.State) ?? false;

                //Are we set to always prompt?
                if (alwaysPrompt)
                {
                    //If so then we should actually clear the users input and prompt again.
                    dc.State.SetValue(AggregationDialogMemory, string.Empty);
                }
                else
                {
                    //Otherwise we just want to leave.
                    return await dc.EndDialogAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                }
            }

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
    }
}