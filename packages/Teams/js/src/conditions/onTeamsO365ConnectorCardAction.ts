/**
 * @module botbuilder-dialogs-adaptive-teams
 */
/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 * Licensed under the MIT License.
 */

import { Expression } from 'adaptive-expressions';
import { Channels } from 'botbuilder';
import { TurnPath } from 'botbuilder-dialogs';
import { OnInvokeActivity } from 'botbuilder-dialogs-adaptive';

/**
 * Actions triggered when a Teams InvokeActivity is received for 'actionableMessage/executeAction'.
 */
export class OnTeamsO365ConnectorCardAction extends OnInvokeActivity {
    static $kind = 'Teams.OnO365ConnectorCardAction';

    /**
     * Create expression for this condition.
     *
     * @returns {Expression} An [Expression](xref:adaptive-expressions.Expression) used to evaluate this rule.
     */
    protected createExpression(): Expression {
        return Expression.andExpression(
            Expression.parse(
                `${TurnPath.activity}.channelId == '${Channels.Msteams}' && ${TurnPath.activity}.name == 'actionableMessage/executeAction'`
            ),
            super.createExpression()
        );
    }
}
