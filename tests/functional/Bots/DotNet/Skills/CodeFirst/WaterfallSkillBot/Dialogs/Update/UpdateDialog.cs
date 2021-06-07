// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Update
{
    public class UpdateDialog : ComponentDialog
    {
        private readonly List<string> _updateSupported = new List<string>
        {
            Channels.Msteams,
            Channels.Slack,
            Channels.Telegram
        };

        private readonly Dictionary<string, (string, int)> _updateTracker;

        public UpdateDialog()
            : base(nameof(UpdateDialog))
        {
            _updateTracker = new Dictionary<string, (string, int)>();
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[] { HandleUpdateDialog, FinalStepAsync }));
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> HandleUpdateDialog(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var channel = stepContext.Context.Activity.ChannelId;
            if (_updateSupported.Contains(channel))
            {
                if (_updateTracker.ContainsKey(stepContext.Context.Activity.Conversation.Id))
                {
                    var conversationId = stepContext.Context.Activity.Conversation.Id;
                    var tuple = _updateTracker[conversationId];
                    var activity = MessageFactory.Text($"This message has been updated {tuple.Item2} time(s).");
                    tuple.Item2 += 1;
                    activity.Id = tuple.Item1;
                    _updateTracker[conversationId] = tuple;
                    await stepContext.Context.UpdateActivityAsync(activity, cancellationToken);
                }
                else
                {
                    var id = await stepContext.Context.SendActivityAsync(MessageFactory.Text("Here is the original activity"), cancellationToken);
                    _updateTracker.Add(stepContext.Context.Activity.Conversation.Id, (id.Id, 1));
                }
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Delete is not supported in the {channel} channel."), cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Complete);
            }

            // Ask if we want to update the activity again.
            const string messageText = "Do you want to update the activity again?";
            const string repromptMessageText = "Please select a valid answer";
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText),
                RetryPrompt = MessageFactory.Text(repromptMessageText, repromptMessageText),
            };

            // Ask the user to enter their name.
            return await stepContext.PromptAsync(nameof(ConfirmPrompt), options, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var tryAnother = (bool)stepContext.Result;
            if (tryAnother)
            {
                return await stepContext.ReplaceDialogAsync(InitialDialogId, cancellationToken: cancellationToken);
            }

            _updateTracker.Remove(stepContext.Context.Activity.Conversation.Id);
            return new DialogTurnResult(DialogTurnStatus.Complete);
        }
    }
}
