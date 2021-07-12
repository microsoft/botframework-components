// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ComponentDialog, OAuthPrompt, WaterfallDialog } = require('botbuilder-dialogs');

const OAUTH_PROMPT = 'OAuthPrompt';
const WATERFALL_DIALOG = 'WaterfallDialog';

class SsoSkillSignInDialog extends ComponentDialog {
  /**
   * @param {string} dialogId
   * @param {string} connectionName
   */
  constructor (dialogId, connectionName) {
    super(dialogId);

    this.addDialog(new OAuthPrompt(OAUTH_PROMPT, {
      connectionName: connectionName,
      text: 'Sign in to the Skill using AAD',
      title: 'Sign In'
    }));

    this.addDialog(new WaterfallDialog(WATERFALL_DIALOG, [
      this.signInStep.bind(this),
      this.displayToken.bind(this)
    ]));

    this.initialDialogId = WATERFALL_DIALOG;
  }

  /**
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async signInStep (stepContext) {
    return stepContext.beginDialog(OAUTH_PROMPT);
  }

  /**
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async displayToken (stepContext) {
    const { result } = stepContext;
    if (!result || !result.token) {
      await stepContext.context.sendActivity('No token was provided for the skill.');
    } else {
      await stepContext.context.sendActivity(`Here is your token for the skill: ${result.token}`);
    }

    return stepContext.endDialog();
  }
}

module.exports.SsoSkillSignInDialog = SsoSkillSignInDialog;
