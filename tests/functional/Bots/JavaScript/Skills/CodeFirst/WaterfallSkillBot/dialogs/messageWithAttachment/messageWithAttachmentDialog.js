// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { MessageFactory, InputHints } = require('botbuilder');
const { ComponentDialog, ChoicePrompt, WaterfallDialog, ChoiceFactory, DialogTurnStatus, ConfirmPrompt } = require('botbuilder-dialogs');
const fs = require('fs');
const path = require('path');

const ATTACHMENT_TYPE_PROMPT = 'AttachmentTypePrompt';
const CONFIRM_PROMPT = 'ConfirmPrompt';
const WATERFALL_DIALOG = 'WaterfallDialog';

class MessageWithAttachmentDialog extends ComponentDialog {
  /**
   * @param {string} dialogId
   * @param {string} serverUrl
   */
  constructor (dialogId, serverUrl) {
    super(dialogId);

    this.picture = 'architecture-resize.png';
    this.serverUrl = serverUrl;

    this.addDialog(new ChoicePrompt(ATTACHMENT_TYPE_PROMPT))
      .addDialog(new ConfirmPrompt(CONFIRM_PROMPT))
      .addDialog(new WaterfallDialog(WATERFALL_DIALOG, [
        this.selectAttachmentType.bind(this),
        this.sendActivityWithAttachment.bind(this),
        this.finalStep.bind(this)
      ]));

    this.initialDialogId = WATERFALL_DIALOG;
  }

  /**
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async selectAttachmentType (stepContext) {
    const messageText = 'What attachment type do you want?';
    const repromptMessageText = 'That was not a valid choice, please select a valid card type.';
    const options = {
      prompt: MessageFactory.text(messageText, messageText, InputHints.ExpectingInput),
      retryPrompt: MessageFactory.text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
      choices: ChoiceFactory.toChoices(['Inline', 'Internet'])
    };

    return stepContext.prompt(ATTACHMENT_TYPE_PROMPT, options);
  }

  /**
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async sendActivityWithAttachment (stepContext) {
    const attachmentType = stepContext.result.value.toLowerCase();
    const reply = MessageFactory.text('', '', InputHints.IgnoringInput);

    switch (attachmentType) {
      case 'inline':
        reply.text = 'This is an inline attachment.';
        reply.attachments = [this.getInlineAttachment()];
        break;

      case 'internet':
        reply.text = 'This is an attachment from a HTTP URL.';
        reply.attachments = [this.getInternetAttachment()];
        break;

      default:
        throw new Error(`Invalid card type ${attachmentType}`);
    }

    await stepContext.context.sendActivity(reply);

    // Ask to submit another or end.
    const messageText = 'Do you want another type of attachment?';
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

  /**
   * Returns an inline attachment.
   * @returns {import('botbuilder').Attachment}
   */
  getInlineAttachment () {
    const filepath = path.resolve(process.cwd(), 'images', this.picture);
    const file = fs.readFileSync(filepath, 'base64');
    return {
      name: `Files/${this.picture}`,
      contentType: 'image/png',
      contentUrl: `data:image/png;base64,${file}`
    };
  }

  /**
   * Returns an attachment to be sent to the user from a HTTPS URL.
   * @returns {import('botbuilder').Attachment}
   */
  getInternetAttachment () {
    return {
      name: `Files/${this.picture}`,
      contentType: 'image/png',
      contentUrl: `${this.serverUrl}/images/architecture-resize.png`
    };
  }
}

module.exports.MessageWithAttachmentDialog = MessageWithAttachmentDialog;
