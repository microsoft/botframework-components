// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { TurnContext, MessageFactory, ActivityTypes, ActivityEventNames, EndOfConversationCodes } = require('botbuilder');
const { Dialog, DialogTurnStatus } = require('botbuilder-dialogs');

class WaitForProactiveDialog extends Dialog {
  /**
   * @param {string} dialogId
   * @param {string} serverUrl
   * @param {Object<string, import('./continuationParameters').ContinuationParameters>} continuationParametersStore
   */
  constructor (dialogId, serverUrl, continuationParametersStore) {
    super(dialogId);
    this.serverUrl = serverUrl;
    this.continuationParametersStore = continuationParametersStore;
  }

  /**
   * Message to send to users when the bot receives a Conversation Update event
   * @param {string} url
   * @param {string} id
   */
  notifyMessage (url, id) {
    return `Navigate to ${url}/api/notify?user=${id} to proactively message the user.`;
  }

  /**
   * @param {import('botbuilder-dialogs').DialogContext} dc
   */
  async beginDialog (dc) {
    // Store a reference to the conversation.
    this.addOrUpdateContinuationParameters(dc.context);

    // Render message with continuation link.
    await dc.context.sendActivity(MessageFactory.text(this.notifyMessage(this.serverUrl, dc.context.activity.from.id)));
    return Dialog.EndOfTurn;
  }

  /**
   * @param {import('botbuilder-dialogs').DialogContext} dc
   */
  async continueDialog (dc) {
    const { activity } = dc.context;
    if (activity.type === ActivityTypes.Event && activity.name === ActivityEventNames.ContinueConversation) {
      // We continued the conversation, forget the proactive reference.
      this.continuationParametersStore[activity.from.id] = undefined;

      // The continue conversation activity comes from the ProactiveController when the notification is received
      await dc.context.sendActivity('We received a proactive message, ending the dialog');

      // End the dialog so the host gets an EoC
      await dc.context.sendActivity({
        type: ActivityTypes.EndOfConversation,
        code: EndOfConversationCodes.CompletedSuccessfully
      });
      return { status: DialogTurnStatus.complete };
    }

    // Keep waiting for a call to the ProactiveController.
    await dc.context.sendActivity(`We are waiting for a proactive message. ${this.notifyMessage(this.serverUrl, activity.from.id)}`);

    return Dialog.EndOfTurn;
  }

  /**
   * Helper to extract and store parameters we need to continue a conversation from a proactive message.
   * @param {import('botbuilder').TurnContext} turnContext
   */
  addOrUpdateContinuationParameters (turnContext) {
    this.continuationParametersStore[turnContext.activity.from.id] = {
      claimsIdentity: turnContext.turnState.get(turnContext.adapter.BotIdentityKey),
      conversationReference: TurnContext.getConversationReference(turnContext.activity),
      oAuthScope: turnContext.turnState.get(turnContext.adapter.OAuthScopeKey)
    };
  }
}

module.exports.WaitForProactiveDialog = WaitForProactiveDialog;
