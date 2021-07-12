// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { MessageFactory, InputHints } = require('botbuilder');
const { ComponentDialog, ChoicePrompt, ChoiceFactory, DialogTurnStatus, WaterfallDialog } = require('botbuilder-dialogs');
const { SsoSkillSignInDialog } = require('./SsoSkillSignInDialog');

const ACTION_PROMPT = 'ActionStepPrompt';
const WATERFALL_DIALOG = 'WaterfallDialog';
const SSO_SKILL_DIALOG = 'SsoSkillDialog';

class SsoSkillDialog extends ComponentDialog {
  /**
   * @param {string} dialogId
   * @param {Object} configuration
   */
  constructor (dialogId, configuration) {
    super(dialogId);

    this.connectionName = configuration.SsoConnectionName;

    this.addDialog(new SsoSkillSignInDialog(SSO_SKILL_DIALOG, this.connectionName))
      .addDialog(new ChoicePrompt(ACTION_PROMPT))
      .addDialog(new WaterfallDialog(WATERFALL_DIALOG, [
        this.promptActionStep.bind(this),
        this.handleActionStep.bind(this),
        this.promptFinalStep.bind(this)
      ]));

    this.initialDialogId = WATERFALL_DIALOG;
  }

  /**
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async promptActionStep (stepContext) {
    const messageText = 'What SSO action would you like to perform on the skill?';
    const repromptMessageText = 'That was not a valid choice, please select a valid choice.';

    return stepContext.prompt(ACTION_PROMPT, {
      prompt: MessageFactory.text(messageText, messageText, InputHints.ExpectingInput),
      retryPrompt: MessageFactory.text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
      choices: ChoiceFactory.toChoices(await this.getPromptChoices(stepContext))
    });
  }

  /**
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async getPromptChoices (stepContext) {
    const choices = [];
    const token = await stepContext.context.adapter.getUserToken(stepContext.context, this.connectionName);

    if (!token) {
      choices.push('Login');
    } else {
      choices.push('Logout');
      choices.push('Show token');
    }

    choices.push('End');

    return choices;
  }

  /**
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async handleActionStep (stepContext) {
    const action = stepContext.result.value.toLowerCase();

    switch (action) {
      case 'login':
        return stepContext.beginDialog(SSO_SKILL_DIALOG);

      case 'logout':
        await stepContext.context.adapter.signOutUser(stepContext.context, this.connectionName);
        await stepContext.context.sendActivity('You have been signed out.');
        return stepContext.next();

      case 'show token': {
        const token = await stepContext.context.adapter.getUserToken(stepContext.context, this.connectionName);

        if (!token) {
          await stepContext.context.sendActivity('User has no cached token.');
        } else {
          await stepContext.context.sendActivity(`Here is your current SSO token: ${token.token}`);
        }

        return stepContext.next();
      }

      case 'end':
        return { status: DialogTurnStatus.complete };

      default:
        // This should never be hit since the previous prompt validates the choice.
        throw new Error(`Unrecognized action: ${action}`);
    }
  }

  /**
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async promptFinalStep (stepContext) {
    // Restart the dialog (we will exit when the user says end).
    return stepContext.replaceDialog(this.initialDialogId);
  }
}

module.exports.SsoSkillDialog = SsoSkillDialog;
