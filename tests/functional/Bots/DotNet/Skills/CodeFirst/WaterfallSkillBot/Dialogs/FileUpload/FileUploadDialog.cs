// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.FileUpload
{
    public class FileUploadDialog : ComponentDialog
    {
        public FileUploadDialog()
            : base(nameof(FileUploadDialog))
        {
            AddDialog(new AttachmentPrompt(nameof(AttachmentPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[] { PromptUploadStepAsync, HandleAttachmentStepAsync, FinalStepAsync }));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PromptUploadStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var promptOptions = new PromptOptions
            {
                Prompt = MessageFactory.Text("Please upload a file to continue."),
                RetryPrompt = MessageFactory.Text("You must upload a file."),
            };

            return await stepContext.PromptAsync(nameof(AttachmentPrompt), promptOptions, cancellationToken);
        }

        private async Task<DialogTurnResult> HandleAttachmentStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var fileText = string.Empty;

            foreach (var file in stepContext.Context.Activity.Attachments)
            {
                var remoteFileUrl = file.ContentUrl;
                var localFileName = Path.Combine(Path.GetTempPath(), file.Name);
                string fileContent;

                using (var webClient = new WebClient())
                {
                    webClient.DownloadFile(remoteFileUrl, localFileName);
                    using var reader = new StreamReader(localFileName);
                    fileContent = await reader.ReadToEndAsync();
                }

                fileText += $"Attachment \"{file.Name}\" has been received.\r\n";
                fileText += $"File content: {fileContent}\r\n";
            }

            await stepContext.Context.SendActivityAsync(MessageFactory.Text(fileText), cancellationToken);

            // Ask to upload another file or end.
            const string messageText = "Do you want to upload another file?";
            const string repromptMessageText = "That's an invalid choice.";
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput)
            };

            return await stepContext.PromptAsync(nameof(ConfirmPrompt), options, cancellationToken);
        }

        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var tryAnother = (bool)stepContext.Result;
            if (tryAnother)
            {
                return await stepContext.ReplaceDialogAsync(InitialDialogId, cancellationToken: cancellationToken);
            }

            return new DialogTurnResult(DialogTurnStatus.Complete);
        }
    }
}
