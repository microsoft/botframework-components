// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

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
        /// Gets or sets the name of the command.
        /// </summary>
        /// <value>
        /// <see cref="StringExpression"/>.
        /// </value>
        protected StringExpression Name { get; set; }

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

            var startRecordingActivity = CreateCommandActivity<T>(dc.Context, this.Data, this.Name.GetValue(dc.State));

            var response = await dc.Context.SendActivityAsync(startRecordingActivity, cancellationToken).ConfigureAwait(false);

            // TODO: Save command id / activity id for correlation with the command result.

            return new DialogTurnResult(DialogTurnStatus.Waiting, startRecordingActivity.Name);
        }

        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default)
        {
            // check activity
            var activity = dc.Context.Activity;

            // If command result, handle it
            if (activity.Type == ActivityTypes.CommandResult
                && activity.Name == this.Name.GetValue(dc.State))
            {
                // TODO: correlate command id before handling it.

                var commandResult = TelephonyExtensions.GetCommandResultValue(activity);

                if (commandResult.Error != null)
                {
                    throw new ErrorResponseException($"{commandResult.Error.Code}: {commandResult.Error.Message}");
                }

                return await dc.EndDialogAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            // for now, end turn and keep waiting
            // TODO: Carlos add interruption model
            return new DialogTurnResult(DialogTurnStatus.Waiting);
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
            
            // TODO: Check with SDK crew
            //commandActivity.RelatesTo = turnContext.Activity.GetConversationReference();
            
            commandActivity.ReplyToId = turnContext.Activity.Id;
            commandActivity.ServiceUrl = turnContext.Activity.ServiceUrl;
            commandActivity.ChannelId = turnContext.Activity.ChannelId;

            return commandActivity;
        }
    }
}
