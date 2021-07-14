// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { Channels } = require('botbuilder');
const { CardOptions } = require('./cardOptions');

/**
 * This tracks what cards are not supported in a given channel.
 */
const unsupportedChannelCards = {
  [Channels.Emulator]: [
    CardOptions.AdaptiveCardTeamsTaskModule,
    CardOptions.AdaptiveUpdate,
    CardOptions.TeamsFileConsent,
    CardOptions.O365
  ],
  [Channels.Directline]: [
    CardOptions.AdaptiveUpdate
  ],
  [Channels.Telegram]: [
    CardOptions.AdaptiveCardBotAction,
    CardOptions.AdaptiveCardTeamsTaskModule,
    CardOptions.AdaptiveCardSubmitAction,
    CardOptions.List,
    CardOptions.TeamsFileConsent
  ]
};

class ChannelSupportedCards {
  /**
   * This let's you know if a card is supported in a given channel.
   * @param {string} channel Bot Connector Channel.
   * @param {keyof CardOptions} type Card Option to be checked.
   * @returns A bool if the card is supported in the channel.
   */
  static isCardSupported (channel, type) {
    const unsupportedChannel = unsupportedChannelCards[channel];
    if (unsupportedChannel) {
      return !unsupportedChannel.includes(type);
    }

    return true;
  }
}

module.exports.ChannelSupportedCards = ChannelSupportedCards;
