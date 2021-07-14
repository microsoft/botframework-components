// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import {
  BoolExpression,
  BoolExpressionConverter,
  StringExpressionConverter,
} from 'adaptive-expressions';
import {
  Activity,
  ActivityTypes,
  InvokeResponse,
  StatusCodes,
  TabResponse,
  TabResponseCard,
} from 'botbuilder';
import {
  Converter,
  ConverterFactory,
  Dialog,
  DialogContext,
  DialogStateManager,
  DialogTurnResult,
  TemplateInterface,
} from 'botbuilder-dialogs';
import { ActivityTemplateConverter } from 'botbuilder-dialogs-adaptive/lib/converters';

export interface SendTabCardResponseConfiguration extends Dialog {
  cards?: TemplateInterface<Activity, DialogStateManager>;
  disabled?: boolean | string | BoolExpression;
}

/**
 * Send a Card Tab response to the user.
 */
export class SendTabCardResponse extends Dialog {
  /**
   * Class identifier.
   */
  public static $kind = 'Teams.SendTabCardResponse';

  /**
   * Gets or sets an optional expression which if is true will disable this action.
   */
  public disabled?: BoolExpression;

  /**
   * Template for the activity expression containing Adaptive Cards to send.
   */
  public cards?: TemplateInterface<Activity, DialogStateManager>;

  public getConverter(
    property: keyof Dialog | string
  ): Converter | ConverterFactory {
    switch (property) {
      case 'disabled':
        return new BoolExpressionConverter();
      case 'property':
        return new StringExpressionConverter();
      case 'cards':
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

    if (!this.cards) {
      throw new Error(
        `Valid Cards are required for ${SendTabCardResponse.$kind}.`
      );
    }

    const activity = await this.cards?.bind(dc, dc.state);

    if (!activity?.attachments?.length) {
      throw new Error(
        `Invalid activity. Attachment(s) are required for ${SendTabCardResponse.$kind}.`
      );
    }

    const cards = activity.attachments.map(
      (attachment): TabResponseCard => {
        return {
          card: attachment.content,
        };
      }
    );
    const responseActivity = this.getTabInvokeResponse(cards);

    const sendResponse = await dc.context.sendActivity(responseActivity);
    return dc.endDialog(sendResponse);
  }

  /**
   * Builds the compute Id for the dialog.
   *
   * @returns {string} A string representing the compute Id.
   */
  protected onComputeId(): string {
    return `SendTabCardResponse[\
            ${this.cards?.toString() ?? ''}\
        ]`;
  }

  private getTabInvokeResponse(cards: TabResponseCard[]): Partial<Activity> {
    return {
      value: <InvokeResponse>{
        status: StatusCodes.OK,
        body: <TabResponse>{
          tab: {
            type: 'continue',
            value: {
              cards,
            },
          },
        },
      },
      type: ActivityTypes.InvokeResponse,
    };
  }
}
