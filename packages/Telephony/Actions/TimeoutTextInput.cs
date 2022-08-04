using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Components.Telephony.Common;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Components.Telephony.Actions
{
    public class TimeoutTextInput : TextInput, ITimeoutInput
    {
        [JsonProperty("$kind")]
        public new const string Kind = "Microsoft.Telephony.TimeoutTextInput";

        /// <summary>
        /// Gets or sets a value indicating how long to wait for before timing out and using the default value.
        /// </summary>
        [JsonProperty("timeOutInMilliseconds")]
        public IntExpression TimeOutInMilliseconds { get; set; }

        [JsonConstructor]
        public TimeoutTextInput([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            : base()
        {
            // enable instances of this command as debug break point
            this.RegisterSourceLocation(sourceFilePath, sourceLineNumber);
        }

        public async override Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await TimeoutInput.BeginDialogAsync(this, dc,
                base.BeginDialogAsync,
                options, cancellationToken);
        }

        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await TimeoutInput.ContinueDialogAsync(this, dc, VALUE_PROPERTY, TURN_COUNT_PROPERTY,
                 OnRecognizeInputAsync,
                 base.ContinueDialogAsync,
                 cancellationToken);
        }

    }
}