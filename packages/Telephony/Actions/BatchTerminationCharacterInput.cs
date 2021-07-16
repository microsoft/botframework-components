// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;

namespace Microsoft.Bot.Components.Telephony.Actions
{
    /// <summary>
    /// Aggregates input until it matches a regex pattern and then stores the result in an output property.
    /// </summary>
    public class BatchTerminationCharacterInput : RegexAggregatorInput
    {
        /// <summary>
        /// Class identifier.
        /// </summary>
        [JsonProperty("$kind")]
        public new const string Kind = "Microsoft.Telephony.TerminationCharacterBatchInput";

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
        public override Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return base.BeginDialogAsync(dc, options, cancellationToken);
        }

        /// <inheritdoc/>
        public override Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default)
        {
            return base.ContinueDialogAsync(dc, cancellationToken);
        }
    }
}