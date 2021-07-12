// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import { Expression } from 'adaptive-expressions';
import { Channels } from 'botbuilder';
import { TurnPath } from 'botbuilder-dialogs';
import { OnEventActivity } from 'botbuilder-dialogs-adaptive';

/**
 * Actions triggered when a Teams Meeting Start event is received.
 * Note: turn.activity.value has meeting data.
 */
export class OnTeamsMeetingStart extends OnEventActivity {
  static $kind = 'Teams.OnMeetingStart';

  /**
   * Create expression for this condition.
   *
   * @returns {Expression} An [Expression](xref:adaptive-expressions.Expression) used to evaluate this rule.
   */
  protected createExpression(): Expression {
    return Expression.andExpression(
      Expression.parse(
        `${TurnPath.activity}.channelId == '${Channels.Msteams}' && ${TurnPath.activity}.name == 'application/vnd.microsoft.meetingStart'`
      ),
      super.createExpression()
    );
  }
}
