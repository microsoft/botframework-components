// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { MessageFactory, InputHints } = require('botbuilder');
const { ComponentDialog, AttachmentPrompt, WaterfallDialog, DialogTurnStatus, ConfirmPrompt } = require('botbuilder-dialogs');
const fs = require('fs');
const fetch = require('node-fetch');
const os = require('os');
const path = require('path');
const stream = require('stream');
const util = require('util');

const streamPipeline = util.promisify(stream.pipeline);

const ATTACHMENT_PROMPT = 'AttachmentPrompt';
const CONFIRM_PROMPT = 'ConfirmPrompt';
const WATERFALL_DIALOG = 'WaterfallDialog';

class FileUploadDialog extends ComponentDialog {
  /**
   * @param {string} dialogId
   */
  constructor (dialogId) {
    super(dialogId);

    this.addDialog(new AttachmentPrompt(ATTACHMENT_PROMPT))
      .addDialog(new ConfirmPrompt(CONFIRM_PROMPT))
      .addDialog(new WaterfallDialog(WATERFALL_DIALOG, [
        this.promptUploadStep.bind(this),
        this.handleAttachmentStep.bind(this),
        this.finalStep.bind(this)
      ]));

    this.initialDialogId = WATERFALL_DIALOG;
  }

  /**
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async promptUploadStep (stepContext) {
    return stepContext.prompt(ATTACHMENT_PROMPT, {
      prompt: MessageFactory.text('Please upload a file to continue.'),
      retryPrompt: MessageFactory.text('You must upload a file.')
    });
  }

  /**
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async handleAttachmentStep (stepContext) {
    let fileText = '';
    let fileContent = '';

    for (const file of stepContext.context.activity.attachments) {
      const localFileName = path.resolve(os.tmpdir(), file.name);
      const tempFile = fs.createWriteStream(localFileName);
      const response = await fetch(file.contentUrl);
      await streamPipeline(response.body, tempFile);

      fileContent = fs.readFileSync(localFileName, 'utf8');
      fileText += `Attachment "${file.name}" has been received.\r\n`;
      fileText += `File content: ${fileContent}\r\n`;
    }

    await stepContext.context.sendActivity(MessageFactory.text(fileText));

    // Ask to upload another file or end.
    const messageText = 'Do you want to upload another file?';
    const repromptMessageText = "That's an invalid choice.";

    return stepContext.prompt(CONFIRM_PROMPT, {
      prompt: MessageFactory.text(messageText, messageText, InputHints.ExpectingInput),
      retryPrompt: MessageFactory.text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput)
    });
  }

  /**
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async finalStep (stepContext) {
    const tryAnother = stepContext.result;

    if (tryAnother) {
      return stepContext.replaceDialog(this.initialDialogId);
    }

    return { status: DialogTurnStatus.complete };
  }
}

module.exports.FileUploadDialog = FileUploadDialog;
