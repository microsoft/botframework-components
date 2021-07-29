// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Components.Telephony.Common
{
    /// <summary>
    /// Generic dialog to orchestrate issuing command activities and releasing control once a command result is received.
    /// </summary>
    /// <remarks>
    /// TODO: Command.Value.Data and CommandResult.Value.Data can be of two different types
    /// Potentially need T1 and T2.
    /// </remarks>
    /// <typeparam name="T">Type of data stored in the <see <see cref="CommandValue{T}"/>.</typeparam>
    public class CommandDialog<T> : Dialog
    {
        /// <summary>
        /// Gets or sets intteruption policy. 
        /// </summary>
        /// <value>
        /// Bool or expression which evalutes to bool.
        /// </value>
        [JsonProperty("allowInterruptions")]
        public BoolExpression AllowInterruptions { get; set; } = true;

        /// <summary>
        /// Gets or sets the name of the command.
        /// </summary>
        /// <value>
        /// <see cref="StringExpression"/>.
        /// </value>
        protected StringExpression CommandName { get; set; }

        /// <summary>
        /// Gets or sets the data payload of the command.
        /// </summary>
        /// <value>
        /// <see cref="StringExpression"/>.
        /// </value>
        protected T Data { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            // TODO: check name not null / expression has value
            var startRecordingActivity = CreateCommandActivity<T>(dc.Context, this.Data, this.CommandName.GetValue(dc.State));

            var response = await dc.Context.SendActivityAsync(startRecordingActivity, cancellationToken).ConfigureAwait(false);

            // TODO: Save command id / activity id for correlation with the command result.

            return new DialogTurnResult(DialogTurnStatus.Waiting, startRecordingActivity.Name);
        }

        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default)
        {
            var activity = dc.Context.Activity;

            // We are expecting a command result with the same name as our current CommandName.
            if (activity.Type == ActivityTypes.CommandResult
                && activity.Name == this.CommandName.GetValue(dc.State))
            {
                // TODO: correlate command id before handling it.

                var commandResult = TelephonyExtensions.GetCommandResultValue(activity);

                if (commandResult?.Error != null)
                {
                    throw new ErrorResponseException($"{commandResult.Error.Code}: {commandResult.Error.Message}");
                }

                return await dc.EndDialogAsync(
                    new DialogTurnResult(DialogTurnStatus.Complete),
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            // This activity was not the command result we were expecting. Mark as waiting and end the turn.
            return new DialogTurnResult(DialogTurnStatus.Waiting);
        }

        /// <inheritdoc/>
        protected override async Task<bool> OnPreBubbleEventAsync(DialogContext dc, DialogEvent e, CancellationToken cancellationToken)
        {
            if (e.Name == DialogEvents.ActivityReceived && dc.Context.Activity.Type == ActivityTypes.Message)
            {
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

        private static Activity CreateCommandActivity<TValue>(ITurnContext turnContext, T data, string name)
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var commandActivity = new Activity(type: ActivityTypes.Command);

            commandActivity.Name = name;

            var commandValue = new CommandValue<T>()
            {
                CommandId = Guid.NewGuid().ToString(),
                Data = data,
            };

            commandActivity.Value = commandValue;

            commandActivity.From = turnContext.Activity.From;
                    
            commandActivity.ReplyToId = turnContext.Activity.Id;
            commandActivity.ServiceUrl = turnContext.Activity.ServiceUrl;
            commandActivity.ChannelId = turnContext.Activity.ChannelId;

            return commandActivity;
        }
    }
}
