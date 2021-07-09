// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

/**
 * Stores the information needed to resume a conversation when a proactive message arrives.
 */
class ContinuationParameters {
  /**
   * @param {string} claimsIdentity
   * @param {string} oAuthScope
   * @param {import('botbuilder').ConversationReference} conversationReference
   */
  constructor (claimsIdentity, oAuthScope, conversationReference) {
    this.claimsIdentity = claimsIdentity;
    this.oAuthScope = oAuthScope;
    this.conversationReference = conversationReference;
  }
}

module.exports.ContinuationParameters = ContinuationParameters;
