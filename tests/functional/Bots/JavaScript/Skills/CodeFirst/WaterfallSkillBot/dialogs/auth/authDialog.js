// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { InputHints, MessageFactory } = require('botbuilder');
const { ComponentDialog, OAuthPrompt, WaterfallDialog, ConfirmPrompt } = require('botbuilder-dialogs');

const WATERFALL_DIALOG = 'WaterfallDialog';
const OAUTH_PROMPT = 'OAuthPrompt';
const CONFIRM_PROMPT = 'ConfirmPrompt';

class AuthDialog extends ComponentDialog {
  /**
   * @param {string} dialogId
   * @param {Object} configuration
   */
  constructor (dialogId, configuration) {
    super(dialogId);

    this.connectionName = configuration.ConnectionName;

    this.addDialog(new ConfirmPrompt(CONFIRM_PROMPT))
      .addDialog(new OAuthPrompt(OAUTH_PROMPT, {
        connectionName: this.connectionName,
        text: `Please Sign In to connection: '${this.connectionName}'`,
        title: 'Sign In',
        timeout: 300000 // User has 5 minutes to login (1000 * 60 * 5)
      }));

    this.addDialog(new WaterfallDialog(WATERFALL_DIALOG, [
      this.promptStep.bind(this),
      this.loginStep.bind(this),
      this.displayToken.bind(this)
    ]));

    // The initial child Dialog to run.
    this.initialDialogId = WATERFALL_DIALOG;
  }

  /**
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  promptStep (stepContext) {
    return stepContext.beginDialog(OAUTH_PROMPT);
  }

  /**
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async loginStep (stepContext) {
    // Get the token from the previous step.
    const tokenResponse = stepContext.result;

    if (tokenResponse) {
      stepContext.values.token = tokenResponse.token;

      // Show the token
      const loggedInMessage = 'You are now logged in.';
      await stepContext.context.sendActivity(MessageFactory.text(loggedInMessage, loggedInMessage, InputHints.IgnoringInput));

      return stepContext.prompt(CONFIRM_PROMPT, {
        prompt: MessageFactory.text('Would you like to view your token?')
      });
    }

    const tryAgainMessage = 'Login was not successful please try again.';
    await stepContext.context.sendActivity(MessageFactory.text(tryAgainMessage, tryAgainMessage, InputHints.IgnoringInput));
    return stepContext.replaceDialog(this.initialDialogId);
  }

  /**
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async displayToken (stepContext) {
    const result = stepContext.result;

    if (result) {
      const showTokenMessage = 'Here is your token:';
      await stepContext.context.sendActivity(MessageFactory.text(`${showTokenMessage} ${stepContext.values.token}`, showTokenMessage, InputHints.IgnoringInput));
    }

    // Sign out
    stepContext.context.adapter.signOutUser(stepContext.context, this.connectionName);
    const signOutMessage = 'I have signed you out.';
    await stepContext.context.sendActivity(MessageFactory.text(signOutMessage, signOutMessage, InputHints.IgnoringInput));

    return stepContext.endDialog();
  }
}

module.exports.AuthDialog = AuthDialog;
