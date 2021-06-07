// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { InputHints, MessageFactory } = require('botbuilder');
const { ComponentDialog, DialogSet, TextPrompt, WaterfallDialog, DialogTurnStatus } = require('botbuilder-dialogs');

const TANGENT_DIALOG = 'TangentDialog';
const WATERFALL_DIALOG = 'WaterfallDialog';
const TEXT_PROMPT = 'TextPrompt';

class TangentDialog extends ComponentDialog {
  /**
   * @param {string} dialogId
   */
  constructor (dialogId = TANGENT_DIALOG) {
    super(dialogId);

    this.addDialog(new TextPrompt(TEXT_PROMPT));
    this.addDialog(new WaterfallDialog(WATERFALL_DIALOG, [
      this.step1.bind(this),
      this.step2.bind(this),
      this.endStep.bind(this)
    ]));

    this.initialDialogId = WATERFALL_DIALOG;
  }

  /**
   * The run method handles the incoming activity (in the form of a TurnContext) and passes it through the dialog system.
   * If no dialog is active, it will start the default dialog.
   * @param {import('botbuilder').TurnContext} turnContext
   * @param {*} accessor
   */
  async run (turnContext, accessor) {
    const dialogSet = new DialogSet(accessor);
    dialogSet.add(this);

    const dialogContext = await dialogSet.createContext(turnContext);
    const results = await dialogContext.continueDialog();
    if (results.status === DialogTurnStatus.empty) {
      await dialogContext.beginDialog(this.id);
    }
  }

  /**
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async step1 (stepContext) {
    const messageText = 'Tangent step 1 of 2, say something.';
    const promptMessage = MessageFactory.text(messageText, messageText, InputHints.ExpectingInput);

    return stepContext.prompt(TEXT_PROMPT, promptMessage);
  }

  /**
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async step2 (stepContext) {
    const messageText = 'Tangent step 2 of 2, say something.';
    const promptMessage = MessageFactory.text(messageText, messageText, InputHints.ExpectingInput);

    return stepContext.prompt(TEXT_PROMPT, promptMessage);
  }

  /**
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async endStep (stepContext) {
    return stepContext.endDialog();
  }
}

module.exports.TangentDialog = TangentDialog;
