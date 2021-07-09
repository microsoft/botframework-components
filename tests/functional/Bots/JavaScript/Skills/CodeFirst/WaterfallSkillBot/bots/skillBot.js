// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ActivityHandler, ActivityTypes } = require('botbuilder');
const { runDialog } = require('botbuilder-dialogs');

class SkillBot extends ActivityHandler {
  /**
   * @param {import('botbuilder').ConversationState} conversationState
   * @param {import('botbuilder-dialogs').Dialog} dialog
   * @param {string} serverUrl
   */
  constructor (conversationState, dialog, serverUrl) {
    super();
    if (!conversationState) throw new Error('[SkillBot]: Missing parameter. conversationState is required');
    if (!dialog) throw new Error('[SkillBot]: Missing parameter. dialog is required');

    this.conversationState = conversationState;
    this.dialog = dialog;
    this.serverUrl = serverUrl;

    this.onTurn(async (turnContext, next) => {
      if (turnContext.activity.type !== ActivityTypes.ConversationUpdate) {
        await runDialog(this.dialog, turnContext, this.conversationState.createProperty('DialogState'));
      }

      await next();
    });

    this.onMembersAdded(async (turnContext, next) => {
      const text = 'Welcome to the waterfall skill bot. \n\nThis is a skill, you will need to call it from another bot to use it.';

      for (const member of turnContext.activity.membersAdded) {
        if (member.id !== turnContext.activity.recipient.id) {
          await turnContext.sendActivity({
            type: ActivityTypes.Message,
            text,
            speak: text.replace('\n\n', '')
          });
          await turnContext.sendActivity(`You can check the skill manifest to see what it supports here: ${this.serverUrl}/manifests/waterfallskillbot-manifest-1.0.json`);
        }
      }

      // By calling next() you ensure that the next BotHandler is run.
      await next();
    });
  }

  /**
   * Override the ActivityHandler.run() method to save state changes after the bot logic completes.
   * @param {import('botbuilder').TurnContext} context
   */
  async run (context) {
    await super.run(context);

    // Save any state changes. The load happened during the execution of the Dialog.
    await this.conversationState.saveChanges(context);
  }
}

module.exports.SkillBot = SkillBot;
