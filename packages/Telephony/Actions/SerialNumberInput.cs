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
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Components.Telephony.Actions
{
    /// <summary>
    /// Aggregates input until it matches a SerialNumberPattern and then stores the result in an output property.
    /// </summary>
    public class SerialNumberInput : Dialog
    {
        [JsonProperty("$kind")]
        public const string Kind = "Microsoft.Telephony.SerialNumberInput";
        protected const string AggregationDialogMemory = "this.aggregation";

        /// <summary>
        /// Initializes a new instance of the <see cref="SerialNumberInput"/> class.
        /// </summary>
        /// <param name="sourceFilePath">Optional, source file full path.</param>
        /// <param name="sourceLineNumber">Optional, line number in source file.</param>
        [JsonConstructor]
        public SerialNumberInput([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            : base()
        {
            // enable instances of this command as debug break point
            RegisterSourceLocation(sourceFilePath, sourceLineNumber);
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
        /// Gets or sets if SerialNumberInput should accept alphabet characters. 
        /// </summary>
        /// <value>
        /// Bool or expression which evalutes to bool.
        /// </value>
        [JsonProperty("acceptAlphabet")]
        public BoolExpression AcceptAlphabet { get; set; }

        /// <summary>
        /// Gets or sets if SerialNumberInput should accept numbers. 
        /// </summary>
        /// <value>
        /// Bool or expression which evalutes to bool.
        /// </value>
        [JsonProperty("acceptNumbers")]
        public BoolExpression AcceptNumbers { get; set; }

        /// <summary>
        /// Gets or sets the minimum amount of characters collected before storing the value and ending the dialog.
        /// </summary>
        [JsonProperty("batchLength")]
        public NumberExpression BatchLength { get; set; }

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
        /// Gets or sets the ambiguous confirmation to send to the user.
        /// </summary>
        /// <value>
        /// An activity template.
        /// </value>
        [JsonProperty("confirmationPrompt")]
        public ITemplate<Activity> ConfirmationPrompt { get; set; }

        /// <summary>
        /// Gets or sets the continue prompt once user confirms ambiguous selection.
        /// </summary>
        /// <value>
        /// An activity template.
        /// </value>
        [JsonProperty("continuePrompt")]
        public ITemplate<Activity> ContinuePrompt { get; set; }

        /// <summary>
        /// Gets or sets a regex describing input that should be flagged as handled and not bubble.
        /// </summary>
        /// <value>
        /// String or expression which evaluates to string.
        /// </value>
        [JsonProperty("interruptionMask")]
        public StringExpression InterruptionMask { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            return await PromptUserAsync(dc, cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default)
        {
            //Check if we were interrupted. If we were, follow our logic for when we get interrupted.
            var wasInterrupted = dc.State.GetValue(TurnPath.Interrupted, () => false);
            if (wasInterrupted)
            {
                return await PromptUserAsync(dc, cancellationToken).ConfigureAwait(false);
            }

            //Get value of termination string from expression
            var batchLength = BatchLength.GetValue(dc.State);
            var acceptAlphabet = AcceptAlphabet.GetValue(dc.State);
            var acceptNumbers = AcceptNumbers.GetValue(dc.State);

            // TODO: Delete placeholder regex pattern once we remove SerialNumberPattern.
            var regexPattern = $"([{(acceptAlphabet ? "a-zA-Z" : string.Empty)}{(acceptNumbers ? "0-9" : string.Empty)}]{{{batchLength}}})";
            var snp = new SerialNumberPattern(regexPattern, true);
            var choices = dc.State.GetValue<string[]>("this.ambiguousChoices");
            var isAmbiguousPrompt = choices != null && choices.Length >= 2;
            if (isAmbiguousPrompt)
            {
                dc.State.SetValue("this.ambiguousChoices", null);
                var choice = dc.Context.Activity.Text;
                var result = string.Empty;
                switch (choice)
                {
                    case "1":
                        result = choices[0];
                        break;
                    case "2":
                        result = choices[1];
                        break;
                    default:
                        return await PromptUserAsync(dc, cancellationToken).ConfigureAwait(false);
                }

                if (batchLength == result.Length)
                {
                    //If so, save it to the output property and end the dialog
                    //Get property from expression
                    var property = Property?.GetValue(dc.State);
                    dc.State.SetValue(property, result);
                    return await dc.EndDialogAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    dc.State.SetValue(AggregationDialogMemory, result);
                    await SendActivityMessageAsync(ContinuePrompt, dc, cancellationToken).ConfigureAwait(false);
                    return new DialogTurnResult(DialogTurnStatus.Waiting);
                }
            }

            //If input is longer than pattern length and wasn't handled as an interruption, silently disregard input.
            var userInput = dc.Context.Activity.Text;

            if (userInput.Length > batchLength)
            {
                return new DialogTurnResult(DialogTurnStatus.Waiting);
            }

            //Append the message to the aggregation memory state and check if its length is longer than batch length.
            var existingAggregation = dc.State.GetValue(AggregationDialogMemory, () => string.Empty);

            //Example happy path: PatternLength is 5, AcceptsAlphabet and AcceptsDigits
            //If user says "A", "B", "C", "1", "22", do not update state as "ABC122" is invalid. Wait for them to provide valid input.
            if ((existingAggregation + userInput).Length > batchLength)
            {
                return new DialogTurnResult(DialogTurnStatus.Waiting);
            }

            //TODO: Turn counter? How long is someone in this node?
            //  Scenarios:
            //  1) User keeps providing an incorrect serial number (i.e., the aggregated inputs are too long)
            //  2) User keeps saying "Cancel" on an node configured to be uninterruptable
            //
            //  For 1) "Sorry, the provided serial number was ..." + ("Would you like to try again?" || "Would you like to speak to an agent?")
            //  For 2) When talking to a bot, the user shouldn't enter a stuck state. After a certain number of C1-configurable attempts, the bot should let the user "escape".

            //append the message to the aggregation memory state
            existingAggregation += userInput;

            var results = snp.Inference(existingAggregation);

            //Is the current aggregated message the termination string?
            if (results.Length == 1)
            {
                //If we have a result and its length matches the batch length, set the match on Property and end the dialog.
                if (snp.PatternLength == results[0].Length)
                {
                    //Get property from expression
                    var property = Property?.GetValue(dc.State);
                    dc.State.SetValue(property, results[0]);
                    return await dc.EndDialogAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    //Otherwise update the aggregated inputs and wait for the next input.
                    dc.State.SetValue(AggregationDialogMemory, results[0]);
                    return new DialogTurnResult(DialogTurnStatus.Waiting);
                }
            }
            else if (results.Length >= 2)
            {
                dc.State.SetValue("this.ambiguousChoices", results);
                var promptMsg = ((ActivityTemplate)ConfirmationPrompt).Template.Replace("{0}", results[0]).Replace("{1}", results[1]);
                await dc.Context.SendActivityAsync(promptMsg, promptMsg).ConfigureAwait(false);
                return new DialogTurnResult(DialogTurnStatus.Waiting);
            }
            else
            {
                return new DialogTurnResult(DialogTurnStatus.Waiting);
            }
        }

        /// <inheritdoc/>
        protected override async Task<bool> OnPreBubbleEventAsync(DialogContext dc, DialogEvent e, CancellationToken cancellationToken)
        {
            if (e.Name == DialogEvents.ActivityReceived && dc.Context.Activity.Type == ActivityTypes.Message)
            {
                //Get interruption mask pattern from expression
                var regexPattern = InterruptionMask?.GetValue(dc.State);

                // Return true( already handled ) if input matches our regex interruption mask
                if (!string.IsNullOrEmpty(regexPattern) && Regex.Match(dc.Context.Activity.Text, regexPattern).Success)
                {
                    return true;
                }

                // Ask parent to perform recognition
                await dc.Parent.EmitEventAsync(AdaptiveEvents.RecognizeUtterance, value: dc.Context.Activity, bubble: false, cancellationToken: cancellationToken).ConfigureAwait(false);

                // Should we allow interruptions
                var canInterrupt = true;
                if (AllowInterruptions != null)
                {
                    var (allowInterruptions, error) = AllowInterruptions.TryGetValue(dc.State);
                    canInterrupt = error == null && allowInterruptions;
                }

                // Stop bubbling if interruptions ar NOT allowed
                return !canInterrupt;
            }

            return false;
        }

        private async Task<DialogTurnResult> PromptUserAsync(DialogContext dc, CancellationToken cancellationToken = default)
        {
            //Do we already have a value stored? This would happen in the interruption case, a case in which we are looping over ourselves, or maybe we had a fatal error and had to restart the dialog tree
            var existingAggregation = dc.State.GetValue(AggregationDialogMemory, () => string.Empty);
            if (!string.IsNullOrEmpty(existingAggregation))
            {
                var alwaysPrompt = AlwaysPrompt?.GetValue(dc.State) ?? false;

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

            await SendActivityMessageAsync(Prompt, dc, cancellationToken).ConfigureAwait(false);
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        private async Task SendActivityMessageAsync(ITemplate<Activity> prompt, DialogContext dc, CancellationToken cancellationToken = default)
        {
            var template = prompt ?? throw new InvalidOperationException($"InputDialog is missing Prompt.");
            IMessageActivity msg = await prompt.BindAsync(dc, cancellationToken: cancellationToken).ConfigureAwait(false);
            
            if (msg != null && string.IsNullOrEmpty(msg.InputHint))
            {
                msg.InputHint = InputHints.ExpectingInput;
            }

            var properties = new Dictionary<string, string>()
                {
                    { "template", JsonConvert.SerializeObject(template) },
                    { "result", msg == null ? string.Empty : JsonConvert.SerializeObject(msg, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore, MaxDepth = null }) },
                };
            TelemetryClient.TrackEvent("GeneratorResult", properties);

            await dc.Context.SendActivityAsync(msg, cancellationToken).ConfigureAwait(false);
        }
    }
}
