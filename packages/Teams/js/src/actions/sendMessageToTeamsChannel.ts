// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import {
  BoolExpression,
  BoolExpressionConverter,
  Expression,
  StringExpression,
  StringExpressionConverter,
} from 'adaptive-expressions';
import {
  Activity,
  Channels,
  CloudAdapterBase,
  ConversationReference,
  TeamsInfo,
} from 'botbuilder';
import {
  Converter,
  ConverterFactory,
  Dialog,
  DialogConfiguration,
  DialogContext,
  DialogStateManager,
  DialogTurnResult,
  TemplateInterface,
} from 'botbuilder-dialogs';
import {
  AuthenticationConstants,
  ClaimsIdentity,
  ConnectorClient,
  JwtTokenValidation,
} from 'botframework-connector';
import { ActivityTemplateConverter } from 'botbuilder-dialogs-adaptive/lib/converters';
import {
  getValue,
  testAdapterHasCreateConnectorClientMethod,
} from './actionHelpers';

export interface SendMessageToTeamsChannelConfiguration
  extends DialogConfiguration {
  disabled?: boolean | string | BoolExpression;
  property?: string | Expression | StringExpression;
  conversationReferenceProperty?: string | Expression | StringExpression;
  activityIdProperty?: string | Expression | StringExpression;
  teamsChannelId?: string | Expression | StringExpression;
  activity?: TemplateInterface<Activity, DialogStateManager>;
}

/**
 * Calls TeamsInfo.GetTeamDetails and sets the result to a memory property.
 */
export class SendMessageToTeamsChannel
  extends Dialog
  implements SendMessageToTeamsChannelConfiguration {
  /**
   * Class identifier.
   */
  static $kind = 'Teams.SendMessageToTeamsChannel';

  /**
   * Gets or sets an optional expression which if is true will disable this action.
   */
  public disabled?: BoolExpression;

  /**
   * Gets or sets property path to put the newly created activity's Conversation Reference.
   * This can be used to later send messages to this same conversation.
   */
  public conversationReferenceProperty?: StringExpression;

  /**
   *  Gets or sets property path to put the id of the activity sent.
   */
  public activityIdProperty?: StringExpression;

  /**
   * Gets or sets the expression to get the value to use for team id.
   *
   * @default
   * =turn.activity.channelData.channel.id
   */
  public teamsChannelId: StringExpression = new StringExpression(
    '=turn.activity.channelData.channel.id'
  );

  /**
   * Gets or sets template for the activity expression containing the activity to send.
   */
  public activity?: TemplateInterface<Activity, DialogStateManager>;

  public getConverter(
    property: keyof SendMessageToTeamsChannelConfiguration
  ): Converter | ConverterFactory {
    switch (property) {
      case 'disabled':
        return new BoolExpressionConverter();
      case 'conversationReferenceProperty':
      case 'activityIdProperty':
      case 'teamsChannelId':
        return new StringExpressionConverter();
      case 'activity':
        return new ActivityTemplateConverter();
      default:
        return super.getConverter(property);
    }
  }

  /**
   * Called when the dialog is started and pushed onto the dialog stack.
   *
   * @param {DialogContext} dc The [DialogContext](xref:botbuilder-dialogs.DialogContext) for the current turn of conversation.
   * @param {object} _options Optional, initial information to pass to the dialog.
   * @returns {Promise<DialogTurnResult>} A promise representing the asynchronous operation.
   */
  public async beginDialog(
    dc: DialogContext,
    _options?: Record<string, unknown>
  ): Promise<DialogTurnResult> {
    if (this.disabled?.getValue(dc.state)) {
      return dc.endDialog();
    }

    if (dc.context.activity.channelId !== Channels.Msteams) {
      throw new Error(
        `${SendMessageToTeamsChannel.$kind} works only on the Teams channel.`
      );
    }

    const activity = await this.activity?.bind(dc, dc.state);

    if (!activity) {
      throw new Error(
        `Missing Activity in ${SendMessageToTeamsChannel.$kind}.`
      );
    }

    const teamsChannelId =
      getValue(dc, this.teamsChannelId) ??
      dc.context?.activity.channelData?.channel?.id;

    let result: [ConversationReference, string];

    // Check for legacy adapter
    if (testAdapterHasCreateConnectorClientMethod(dc.context.adapter)) {
      const credentials = dc.context.turnState.get<ConnectorClient>(
        dc.context.adapter.ConnectorClientKey
      )?.credentials;
      if (!credentials) {
        throw new Error(
          'Missing credentials as MicrosoftAppCredentials in ConnectorClient from TurnState'
        );
      }

      result = await TeamsInfo.sendMessageToTeamsChannel(
        dc.context,
        activity,
        teamsChannelId
      );
    } else if (dc.context.adapter instanceof CloudAdapterBase) {
      const botIdentity = dc.context.turnState.get<ClaimsIdentity>(
        dc.context.adapter.BotIdentityKey
      );

      let appId: string | undefined;
      if (botIdentity) {
        // 'version' is sometimes empty, which will result in no id returned from GetAppIdFromClaims.
        appId = JwtTokenValidation.getAppIdFromClaims(botIdentity.claims);

        if (!appId) {
          appId = botIdentity.claims.find(
            (claim) => claim.type === AuthenticationConstants.AudienceClaim
          )?.value;
        }

        if (!appId) {
          throw new Error('Missing AppIdClaim in ClaimsIdentity');
        }
      } else {
        throw new Error(
          'Missing dc.context.adapter.BotIdentityKey in TurnContext TurnState'
        );
      }

      result = await TeamsInfo.sendMessageToTeamsChannel(
        dc.context,
        activity,
        teamsChannelId,
        appId
      );
    } else {
      throw new Error('The adapter does not support SendMessageToTeamsChannel');
    }

    if (this.conversationReferenceProperty != null) {
      dc.state.setValue(
        this.conversationReferenceProperty?.getValue(dc.state),
        result[0]
      );
    }

    if (this.activityIdProperty != null) {
      dc.state.setValue(this.activityIdProperty?.getValue(dc.state), result[1]);
    }

    return dc.endDialog(result);
  }

  /**
   * Builds the compute Id for the dialog.
   *
   * @returns {string} A string representing the compute Id.
   */
  protected onComputeId(): string {
    return `SendMessageToTeamsChannel[\
            ${this.teamsChannelId?.toString() ?? ''},\
            ${this.activityIdProperty?.toString() ?? ''},\
            ${this.conversationReferenceProperty?.toString() ?? ''}\
        ]`;
  }
}
