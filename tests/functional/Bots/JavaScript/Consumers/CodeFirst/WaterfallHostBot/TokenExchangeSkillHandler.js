// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { v4 } = require('uuid');
const { ActivityEx, ActivityTypes, CardFactory, SkillHandler, tokenExchangeOperationName, TurnContext } = require('botbuilder');
const { JwtTokenValidation } = require('botframework-connector');

const WATERFALL_SKILL_BOT = 'WaterfallSkillBot';

/**
 * A SkillHandler specialized to support SSO Token exchanges.
 */
class TokenExchangeSkillHandler extends SkillHandler {
  constructor (adapter, bot, conversationIdFactory, skillsConfig, skillClient,
    credentialProvider, authConfig, channelProvider = null, logger = null) {
    super(adapter, bot, conversationIdFactory, credentialProvider, authConfig, channelProvider);
    this.adapter = adapter;
    this.tokenExchangeProvider = adapter;

    if (!this.tokenExchangeProvider) {
      throw new Error(`${adapter} does not support token exchange`);
    }

    this.skillsConfig = skillsConfig;
    this.skillClient = skillClient;
    this.conversationIdFactory = conversationIdFactory;
    this.logger = logger;
    this.botId = process.env.MicrosoftAppId;
  }

  async onSendToConversation (claimsIdentity, conversationId, activity) {
    if (await this.interceptOAuthCards(claimsIdentity, activity)) {
      return { id: v4() };
    }

    return await super.onSendToConversation(claimsIdentity, conversationId, activity);
  }

  async onReplyToActivity (claimsIdentity, conversationId, activityId, activity) {
    if (await this.interceptOAuthCards(claimsIdentity, activity)) {
      return { id: v4() };
    }

    return await super.onReplyToActivity(claimsIdentity, conversationId, activityId, activity);
  }

  getCallingSkill (claimsIdentity) {
    const appId = JwtTokenValidation.getAppIdFromClaims(claimsIdentity.claims);

    if (!appId) {
      return null;
    }

    return Object.values(this.skillsConfig.skills).find(skill => skill.appId === appId);
  }

  async interceptOAuthCards (claimsIdentity, activity) {
    const oauthCardAttachment = activity.attachments ? activity.attachments.find(attachment => attachment.contentType === CardFactory.contentTypes.oauthCard) : null;
    if (oauthCardAttachment) {
      const targetSkill = this.getCallingSkill(claimsIdentity);
      if (targetSkill) {
        const oauthCard = oauthCardAttachment.content;

        if (oauthCard && oauthCard.tokenExchangeResource && oauthCard.tokenExchangeResource.uri) {
          const context = new TurnContext(this.adapter, activity);
          context.turnState.push('BotIdentity', claimsIdentity);

          // We need to know what connection name to use for the token exchange so we figure that out here
          const connectionName = targetSkill.id.includes(WATERFALL_SKILL_BOT) ? process.env.SsoConnectionName : process.env.SsoConnectionNameTeams;

          if (!connectionName) {
            throw new Error('The connection name cannot be null.');
          }

          // AAD token exchange
          try {
            const result = await this.tokenExchangeProvider.exchangeToken(
              context,
              connectionName,
              activity.recipient.id,
              { uri: oauthCard.tokenExchangeResource.uri }
            );

            if (result.token) {
              // If token above is null, then SSO has failed and hence we return false.
              // If not, send an invoke to the skill with the token.
              return await this.sendTokenExchangeInvokeToSkill(activity, oauthCard.tokenExchangeResource.id, result.token, oauthCard.connectionName, targetSkill);
            }
          } catch (exception) {
            // Show oauth card if token exchange fails.
            this.logger.log('Unable to exchange token.', exception);
            return false;
          }
        }
      }
    }

    return false;
  }

  async sendTokenExchangeInvokeToSkill (incomingActivity, id, token, connectionName, targetSkill) {
    const activity = ActivityEx.createReply(incomingActivity);
    activity.type = ActivityTypes.Invoke;
    activity.name = tokenExchangeOperationName;
    activity.value = {
      id: id,
      token: token,
      connectionName: connectionName
    };

    const skillConversationReference = await this.conversationIdFactory.getSkillConversationReference(incomingActivity.conversation.id);
    activity.conversation = incomingActivity.conversation;
    activity.serviceUrl = skillConversationReference.conversationReference.serviceUrl;

    // Route the activity to the skill
    const response = await this.skillClient.postActivity(this.botId, targetSkill.appId, targetSkill.skillEndpoint, this.skillsConfig.skillHostEndpoint, activity.conversation.id, activity);

    // Check response status: true if success, false if failure
    return response.status >= 200 && response.status <= 299;
  }
}

module.exports.TokenExchangeSkillHandler = TokenExchangeSkillHandler;
