// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.MessageWithAttachment
{
    public class MessageWithAttachmentDialog : ComponentDialog
    {
        private const string Picture = "architecture-resize.png";
        private readonly Uri _serverUrl;

        public MessageWithAttachmentDialog(Uri serverUrl)
            : base(nameof(MessageWithAttachmentDialog))
        {
            _serverUrl = serverUrl;
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[] { SelectAttachmentTypeAsync, SendActivityWithAttachmentAsync, FinalStepAsync }));
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> SelectAttachmentTypeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            const string messageText = "What attachment type do you want?";
            const string repromptMessageText = "That was not a valid choice, please select a valid card type.";
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
                Choices = new List<Choice>
                {
                    new Choice("Inline"),
                    new Choice("Internet")
                }
            };

            // Ask the user to enter their name.
            return await stepContext.PromptAsync(nameof(ChoicePrompt), options, cancellationToken);
        }

        private async Task<DialogTurnResult> SendActivityWithAttachmentAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var attachmentType = ((FoundChoice)stepContext.Result).Value.ToLowerInvariant();
            var reply = new Activity(ActivityTypes.Message) { InputHint = InputHints.IgnoringInput };
            switch (attachmentType)
            {
                case "inline":
                    reply.Text = "This is an inline attachment.";
                    reply.Attachments = new List<Attachment> { GetInlineAttachment() };
                    break;

                case "internet":
                    reply.Text = "This is an attachment from a HTTP URL.";
                    reply.Attachments = new List<Attachment> { GetInternetAttachment() };
                    break;

                default:
                    throw new InvalidOperationException($"Invalid card type {attachmentType}");
            }

            await stepContext.Context.SendActivityAsync(reply, cancellationToken);

            // Ask to submit another or end.
            const string messageText = "Do you want another type of attachment?";
            const string repromptMessageText = "That's an invalid choice.";
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
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

        private Attachment GetInlineAttachment()
        {
            var imagePath = Path.Combine(Environment.CurrentDirectory, "wwwroot", "images", Picture);
            var imageData = Convert.ToBase64String(File.ReadAllBytes(imagePath));

            return new Attachment
            {
                Name = $"Files/{Picture}",
                ContentType = "image/png",
                ContentUrl = $"data:image/png;base64,{imageData}",
            };
        }

        private Attachment GetInternetAttachment()
        {
            return new Attachment
            {
                Name = $"Files/{Picture}",
                ContentType = "image/png",
                ContentUrl = $"{_serverUrl}images/{Picture}",
            };
        }
    }
}
