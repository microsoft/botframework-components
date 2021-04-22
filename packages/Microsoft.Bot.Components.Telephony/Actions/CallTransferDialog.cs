// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Components.Telephony.Actions
{
    public class CallTransferDialog : Dialog
    {
        [JsonConstructor]
        public CallTransferDialog([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            : base()
        {
            // enable instances of this command as debug break point
            this.RegisterSourceLocation(sourceFilePath, sourceLineNumber);
        }

        [JsonProperty("$kind")]
        public const string Kind = "CallTransferDialog";

        [JsonProperty("targetPhoneNumber")]
        public StringExpression TargetPhoneNumber { get; set; }

        public async override Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            var targetPhoneNumber = TargetPhoneNumber.GetValue(dc.State);

            await dc.Context.SendActivityAsync($"Transferring to \"{targetPhoneNumber}\"...");

            // Create handoff event, passing the phone number to transfer to as context.
            var poContext = new { TargetPhoneNumber = targetPhoneNumber };
            var poHandoffEvent = EventFactory.CreateHandoffInitiation(dc.Context, poContext);

            try
            {
                await dc.Context.SendActivityAsync(poHandoffEvent, cancellationToken);
                await dc.Context.SendActivityAsync($"Call transfer initiation succeeded");
            }
            catch
            {
                await dc.Context.SendActivityAsync($"Call transfer failed");
            }

            return await dc.EndDialogAsync(result: 0, cancellationToken: cancellationToken);
        }
    }
}