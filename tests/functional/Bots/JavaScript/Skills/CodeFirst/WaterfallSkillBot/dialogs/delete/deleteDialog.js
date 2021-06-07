// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { MessageFactory } = require('botbuilder');
const { ComponentDialog, WaterfallDialog, DialogTurnStatus } = require('botbuilder-dialogs');
const { Channels } = require('botbuilder-core');

const SLEEP_TIMER = 5000;
const WATERFALL_DIALOG = 'WaterfallDialog';
const DELETE_SUPPORTED = new Set([Channels.Msteams, Channels.Slack, Channels.Telegram]);

class DeleteDialog extends ComponentDialog {
  /**
   * @param {string} dialogId
   */
  constructor (dialogId) {
    super(dialogId);

    this.addDialog(new WaterfallDialog(WATERFALL_DIALOG, [
      this.handleDeleteDialog.bind(this)
    ]));

    this.initialDialogId = WATERFALL_DIALOG;
  }

  /**
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async handleDeleteDialog (stepContext) {
    const channel = stepContext.context.activity.channelId;

    if (DeleteDialog.isDeleteSupported(channel)) {
      const id = await stepContext.context.sendActivity(MessageFactory.text('I will delete this message in 5 seconds'));
      await DeleteDialog.sleep(SLEEP_TIMER);
      await stepContext.context.deleteActivity(id.id);
    } else {
      await stepContext.context.sendActivity(MessageFactory.text(`Delete is not supported in the ${channel} channel.`));
    }

    return { status: DialogTurnStatus.complete };
  }

  /**
   * @param {number} milliseconds
   */
  static sleep (milliseconds) {
    return new Promise(resolve => {
      setTimeout(resolve, milliseconds);
    });
  }

  /**
   * @param {string} channel
   */
  static isDeleteSupported (channel) {
    return DELETE_SUPPORTED.has(channel);
  }
}

module.exports.DeleteDialog = DeleteDialog;
