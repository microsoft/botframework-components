// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { MessageFactory } = require('botbuilder');
const { ConfirmPrompt, ComponentDialog, WaterfallDialog, DialogTurnStatus } = require('botbuilder-dialogs');
const { Channels } = require('botbuilder-core');

const WATERFALL_DIALOG = 'WaterfallDialog';
const CONFIRM_PROMPT = 'ConfirmPrompt';
const UPDATE_SUPPORTED = new Set([Channels.Msteams, Channels.Slack, Channels.Telegram]);

class UpdateDialog extends ComponentDialog {
  /**
   * @param {string} dialogId
   */
  constructor (dialogId) {
    super(dialogId);

    this.updateTracker = {};

    this.addDialog(new ConfirmPrompt(CONFIRM_PROMPT));
    this.addDialog(new WaterfallDialog(WATERFALL_DIALOG, [
      this.handleUpdateDialog.bind(this),
      this.finalStepAsync.bind(this)
    ]));

    this.initialDialogId = WATERFALL_DIALOG;
  }

  /**
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async handleUpdateDialog (stepContext) {
    const channel = stepContext.context.activity.channelId;

    if (UpdateDialog.isUpdateSupported(channel)) {
      const conversationId = stepContext.context.activity.conversation.id;

      if (conversationId in this.updateTracker) {
        const tuple = this.updateTracker[conversationId];
        const activity = MessageFactory.text(`This message has been updated ${tuple[1]} time(s).`);
        activity.id = tuple[0];
        tuple[1]++;
        this.updateTracker[conversationId] = tuple;
        await stepContext.context.updateActivity(activity);
      } else {
        const id = await stepContext.context.sendActivity(MessageFactory.text('Here is the original activity'));
        this.updateTracker[conversationId] = [id.id, 1];
      }
    } else {
      await stepContext.context.sendActivity(MessageFactory.text(`Update is not supported in the ${channel} channel.`));
      return { status: DialogTurnStatus.complete };
    }

    // Ask if we want to update the activity again.
    const messageText = 'Do you want to update the activity again?';
    const repromptMessageText = 'Please select a valid answer';
    const options = {
      prompt: MessageFactory.text(messageText, messageText),
      retryPrompt: MessageFactory.text(repromptMessageText, repromptMessageText)
    };

    // Ask the user to enter a card choice.
    return stepContext.prompt(CONFIRM_PROMPT, options);
  }

  /**
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async finalStepAsync (stepContext) {
    const tryAnother = stepContext.result;

    if (tryAnother) {
      return stepContext.replaceDialog(this.initialDialogId);
    }

    this.updateTracker = {};
    return { status: DialogTurnStatus.complete };
  }

  /**
   * @param {string} channel
   */
  static isUpdateSupported (channel) {
    return UPDATE_SUPPORTED.has(channel);
  }
}

module.exports.UpdateDialog = UpdateDialog;
