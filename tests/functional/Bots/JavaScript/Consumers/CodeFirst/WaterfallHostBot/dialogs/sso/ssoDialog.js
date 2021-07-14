// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ActivityTypes, EndOfConversationCodes, InputHints, MessageFactory } = require('botbuilder');
const { ComponentDialog, ChoicePrompt, ChoiceFactory, DialogTurnStatus, WaterfallDialog } = require('botbuilder-dialogs');
const { SsoSignInDialog } = require('./ssoSignInDialog');

const ACTION_STEP_PROMPT = 'ActionStepPrompt';
const SSO_SIGNIN_DIALOG = 'SsoSignInDialog';
const WATERFALL_DIALOG = 'WaterfallDialog';

/**
 * Helps prepare the host for SSO operations and provides helpers to check the status and invoke the skill.
 */
class SsoDialog extends ComponentDialog {
  /**
   * @param {string} dialogId
   * @param {*} ssoSkillDialog
   * @param {string} connectionName
   */
  constructor (dialogId, ssoSkillDialog, connectionName) {
    super(dialogId);

    this.connectionName = connectionName;
    this.skillDialogId = ssoSkillDialog.id;

    this.addDialog(new ChoicePrompt(ACTION_STEP_PROMPT));
    this.addDialog(new SsoSignInDialog(this.connectionName));
    this.addDialog(ssoSkillDialog);

    this.addDialog(new WaterfallDialog(WATERFALL_DIALOG, [
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
    const messageText = 'What SSO action do you want to perform?';
    const repromptMessageText = 'That was not a valid choice, please select a valid choice.';

    return stepContext.prompt(ACTION_STEP_PROMPT, {
      prompt: MessageFactory.text(messageText, messageText, InputHints.ExpectingInput),
      retryPrompt: MessageFactory.text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
      choices: await this.getPromptChoices(stepContext)
    });
  }

  /**
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async getPromptChoices (stepContext) {
    const promptChoices = [];
    const adapter = stepContext.context.adapter;
    const token = await adapter.getUserToken(stepContext.context, this.connectionName);

    if (!token) {
      promptChoices.push('Login');
      // Token exchange will fail when the host is not logged on and the skill should
      // show a regular OAuthPrompt.
      promptChoices.push('Call Skill (without SSO)');
    } else {
      promptChoices.push('Logout');
      promptChoices.push('Show token');
      promptChoices.push('Call Skill (with SSO)');
    }

    promptChoices.push('Back');

    return ChoiceFactory.toChoices(promptChoices);
  }

  /**
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async handleActionStep (stepContext) {
    const action = stepContext.result.value;

    switch (action.toLowerCase()) {
      case 'login':
        return stepContext.beginDialog(SSO_SIGNIN_DIALOG);

      case 'logout': {
        const adapter = stepContext.context.adapter;
        await adapter.signOutUser(stepContext.context, this.connectionName);
        await stepContext.context.sendActivity('You have been signed out.');
        return stepContext.next();
      }

      case 'show token': {
        const tokenProvider = stepContext.context.adapter;
        const token = await tokenProvider.getUserToken(stepContext.context, this.connectionName);
        if (!token) {
          await stepContext.context.sendActivity('User has no cached token.');
        } else {
          await stepContext.context.sendActivity(`Here is your current SSO token: ${token.token}`);
        }
        return stepContext.next();
      }

      case 'call skill (with sso)':
      case 'call skill (without sso)': {
        const beginSkillActivity = {
          type: ActivityTypes.Event,
          name: 'Sso'
        };
        return stepContext.beginDialog(this.skillDialogId, { activity: beginSkillActivity });
      }

      case 'back':
        await stepContext.context.sendActivity({
          type: ActivityTypes.EndOfConversation,
          code: EndOfConversationCodes.CompletedSuccessfully
        });
        return { status: DialogTurnStatus.complete };

      default:
        // This should never be hit since the previous prompt validates the choice.
        throw new Error(`[SsoDialog]: Unrecognized action: ${action}`);
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

module.exports.SsoDialog = SsoDialog;
