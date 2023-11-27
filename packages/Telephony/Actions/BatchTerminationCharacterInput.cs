// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Components.Telephony.Actions
{
    /// <summary>
    /// Aggregates input until it matches a regex pattern and then stores the result in an output property.
    /// </summary>
    public class BatchTerminationCharacterInput : BatchRegexInput
    {
        /// <summary>
        /// Class identifier.
        /// </summary>
        [JsonProperty("$kind")]
        public new const string Kind = "Microsoft.Telephony.BatchTerminationCharacterInput";

        private const string _dtmfCharacterRegex = @"^[\d#\*]+$";
        private const string _interruptionMaskRegex = @"^[\d]+$";
        private string _terminationCharacter;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchTerminationCharacterInput"/> class.
        /// </summary>
        /// <param name="regexAggregatorInput">Implementation for aggregatingInput.</param>
        /// <param name="sourceFilePath">Optional, source file full path.</param>
        /// <param name="sourceLineNumber">Optional, line number in source file.</param>
        [JsonConstructor]
        public BatchTerminationCharacterInput([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            : base()
        {
            // enable instances of this command as debug break point
            this.RegisterSourceLocation(sourceFilePath, sourceLineNumber);
            InterruptionMask = _interruptionMaskRegex;
        }

        /// <summary>
        /// Gets or sets the character that will be used to signal that the batch of input is complete.
        /// </summary>
        [JsonProperty("terminationCharacter")]
        public string TerminationCharacter
        {
            get
            {
                return _terminationCharacter;
            }

            set
            {
                TerminationConditionRegexPattern = value + "$";
                _terminationCharacter = value;
            }
        }

        /// <inheritdoc/>
        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default)
        {
            if ((dc.Context.Activity.Type == ActivityTypes.Message) &&
            (Regex.Match(dc.Context.Activity.Text, _dtmfCharacterRegex).Success || dc.State.GetValue(TurnPath.Interrupted, () => false)))
            {
                return await base.ContinueDialogAsync(dc, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                if (dc.Context.Activity.Name == ActivityEventNames.ContinueConversation)
                {
                    return await EndDialogAsync(dc, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    return new DialogTurnResult(DialogTurnStatus.Waiting);
                }
            }
        }
    }
}