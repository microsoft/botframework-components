// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import {
  BoolExpression,
  BoolExpressionConverter,
  Expression,
  StringExpression,
  StringExpressionConverter,
} from 'adaptive-expressions';
import { Channels, TeamsInfo } from 'botbuilder';
import {
  Converter,
  ConverterFactory,
  Dialog,
  DialogConfiguration,
  DialogContext,
  DialogTurnResult,
} from 'botbuilder-dialogs';
import { getValue } from './actionHelpers';

export interface GetMeetingInfoConfiguration
  extends DialogConfiguration {
  disabled?: boolean | string | BoolExpression;
  property?: string | Expression | StringExpression;
  meetingId?: string | Expression | StringExpression;
}

/**
 * Calls `TeamsInfo.getMeetingInfo` and sets the result to a memory property.
 */
export class GetMeetingInfo
  extends Dialog
  implements GetMeetingInfoConfiguration {
  /**
   * Class identifier.
   */
  static $kind = 'Teams.GetMeetingInfo';

  /**
   * Gets or sets an optional expression which if is true will disable this action.
   *
   * @example
   * "user.age > 18".
   */
  public disabled?: BoolExpression;

  /**
   * Gets or sets property path to put the value in.
   */
  public property?: StringExpression;

  /**
   * Gets or sets the expression to get the value to use for meeting id.
   *
   * @default
   * =turn.activity.channelData.meeting.id
   */
  public meetingId = new StringExpression(
    '=turn.activity.channelData.meeting.id'
  );

  public getConverter(
    property: keyof GetMeetingInfoConfiguration
  ): Converter | ConverterFactory {
    switch (property) {
      case 'disabled':
        return new BoolExpressionConverter();
      case 'property':
      case 'meetingId':
        return new StringExpressionConverter();
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
        `${GetMeetingInfo.$kind} works only on the Teams channel.`
      );
    }

    const meetingId = getValue(dc, this.meetingId);

    const result = await TeamsInfo.getMeetingInfo(
      dc.context,
      meetingId
    );

    if (this.property != null) {
      dc.state.setValue(this.property.getValue(dc.state), result);
    }

    return dc.endDialog(result);
  }

  /**
   * Builds the compute Id for the dialog.
   *
   * @returns {string} A string representing the compute Id.
   */
  protected onComputeId(): string {
    return `GetMeetingInfo[\
            ${this.meetingId ?? ''},\
            ${this.property?.toString() ?? ''}\
        ]`;
  }
}
