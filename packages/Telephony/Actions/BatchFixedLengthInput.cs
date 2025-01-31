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
    public class BatchFixedLengthInput : BatchRegexInput
    {
        /// <summary>
        /// Class identifier.
        /// </summary>
        [JsonProperty("$kind")]
        public new const string Kind = "Microsoft.Telephony.BatchFixedLengthInput";

        private const string _dtmfCharacterRegex = @"^[\d#\*]+$";
        private const string _interruptionMaskRegex = @"^[\d]+$";
        private int _batchLength;

        /// <summary>
        /// Initializes a new instance of the <see cref="BatchFixedLengthInput"/> class.
        /// </summary>
        /// <param name="regexAggregatorInput">Implementation for aggregatingInput.</param>
        /// <param name="sourceFilePath">Optional, source file full path.</param>
        /// <param name="sourceLineNumber">Optional, line number in source file.</param>
        [JsonConstructor]
        public BatchFixedLengthInput([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            : base()
        {
            // enable instances of this command as debug break point
            this.RegisterSourceLocation(sourceFilePath, sourceLineNumber);
            InterruptionMask = _interruptionMaskRegex;
        }

        /// <summary>
        /// Gets or sets the minimum amount of characters collected before storing the value and ending the dialog.
        /// </summary>
        [JsonProperty("batchLength")]
        public int BatchLength
        {
            get
            {
                return _batchLength;
            }

            set
            {
                TerminationConditionRegexPattern = "(.){" + value + "}";
                _batchLength = value;
            }
        }

        /// <inheritdoc/>
        public override Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.BeginDialogAsync(dc, options, cancellationToken);
        }

        /// <inheritdoc/>
        public async override Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default)
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
