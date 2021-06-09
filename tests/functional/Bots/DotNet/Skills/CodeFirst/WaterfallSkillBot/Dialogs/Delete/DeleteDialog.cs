// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Delete
{
    public class DeleteDialog : ComponentDialog
    {
        private readonly List<string> _deleteSupported = new List<string>
        {
            Channels.Msteams,
            Channels.Slack,
            Channels.Telegram
        };

        public DeleteDialog()
            : base(nameof(DeleteDialog))
        {
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[] { HandleDeleteDialog }));
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> HandleDeleteDialog(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var channel = stepContext.Context.Activity.ChannelId;
            if (_deleteSupported.Contains(channel))
            {
                var id = await stepContext.Context.SendActivityAsync(MessageFactory.Text("I will delete this message in 5 seconds"), cancellationToken);
                await Task.Delay(5000, cancellationToken);
                await stepContext.Context.DeleteActivityAsync(id.Id, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Delete is not supported in the {channel} channel."), cancellationToken);
            }

            return new DialogTurnResult(DialogTurnStatus.Complete);
        }
    }
}
