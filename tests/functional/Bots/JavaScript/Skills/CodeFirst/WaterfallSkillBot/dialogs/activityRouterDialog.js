// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ActivityTypes, InputHints, MessageFactory } = require('botbuilder');
const { DialogTurnStatus, WaterfallDialog, ComponentDialog, SkillDialog } = require('botbuilder-dialogs');
const { AuthDialog } = require('./auth/authDialog');
const { CardDialog } = require('./cards/cardDialog');
const { FileUploadDialog } = require('./fileUpload/fileUploadDialog');
const { MessageWithAttachmentDialog } = require('./messageWithAttachment/messageWithAttachmentDialog');
const { WaitForProactiveDialog } = require('./proactive/waitForProactiveDialog');
const { SsoSkillDialog } = require('./sso/ssoSkillDialog');
const { DeleteDialog } = require('./delete/deleteDialog');
const { UpdateDialog } = require('./update/updateDialog');

const MAIN_DIALOG = 'ActivityRouterDialog';
const WATERFALL_DIALOG = 'WaterfallDialog';
const CARDS_DIALOG = 'Cards';
const PROACTIVE_DIALOG = 'Proactive';
const ATTACHMENT_DIALOG = 'MessageWithAttachment';
const AUTH_DIALOG = 'Auth';
const SSO_DIALOG = 'Sso';
const FILE_UPLOAD_DIALOG = 'FileUpload';
const ECHO_DIALOG = 'Echo';
const DELETE_DIALOG = 'Delete';
const UPDATE_DIALOG = 'Update';

/**
 * A root dialog that can route activities sent to the skill to different sub-dialogs.
 */
class ActivityRouterDialog extends ComponentDialog {
  /**
   * @param {string} serverUrl
   * @param {import('botbuilder').ConversationState} conversationState
   * @param {import('../skillConversationIdFactory').SkillConversationIdFactory} conversationIdFactory
   * @param {import('botbuilder').SkillHttpClient} skillClient
   * @param {Object<string, import('./proactive/continuationParameters').ContinuationParameters>} continuationParametersStore
   */
  constructor (serverUrl, conversationState, conversationIdFactory, skillClient, continuationParametersStore) {
    super(MAIN_DIALOG);

    this.addDialog(new CardDialog(CARDS_DIALOG, serverUrl))
      .addDialog(new WaitForProactiveDialog(PROACTIVE_DIALOG, serverUrl, continuationParametersStore))
      .addDialog(new MessageWithAttachmentDialog(ATTACHMENT_DIALOG, serverUrl))
      .addDialog(new AuthDialog(AUTH_DIALOG, process.env))
      .addDialog(new SsoSkillDialog(SSO_DIALOG, process.env))
      .addDialog(new FileUploadDialog(FILE_UPLOAD_DIALOG))
      .addDialog(new DeleteDialog(DELETE_DIALOG))
      .addDialog(new UpdateDialog(UPDATE_DIALOG))
      .addDialog(this.createEchoSkillDialog(ECHO_DIALOG, conversationState, conversationIdFactory, skillClient, process.env))
      .addDialog(new WaterfallDialog(WATERFALL_DIALOG, [
        this.processActivity.bind(this)
      ]));

    this.initialDialogId = WATERFALL_DIALOG;
  }

  /**
   * @param {string} dialogId
   * @param {import('botbuilder').ConversationState} conversationState
   * @param {import('../skillConversationIdFactory').SkillConversationIdFactory} conversationIdFactory
   * @param {import('botbuilder').SkillHttpClient} skillClient
   * @param {Object} configuration
   */
  createEchoSkillDialog (dialogId, conversationState, conversationIdFactory, skillClient, configuration) {
    const skillHostEndpoint = configuration.SkillHostEndpoint;
    if (!skillHostEndpoint) {
      throw new Error('SkillHostEndpoint is not in configuration');
    }

    const skill = {
      id: configuration.EchoSkillInfo_id,
      appId: configuration.EchoSkillInfo_appId,
      skillEndpoint: configuration.EchoSkillInfo_skillEndpoint
    };

    if (!skill.id || !skill.skillEndpoint) {
      throw new Error('EchoSkillInfo_id and EchoSkillInfo_skillEndpoint are not set in configuration');
    }

    return new SkillDialog({
      botId: configuration.MicrosoftAppId,
      conversationIdFactory,
      skillClient,
      skillHostEndpoint,
      conversationState,
      skill
    }, dialogId);
  }

  /**
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async processActivity (stepContext) {
    // A skill can send trace activities, if needed.
    await stepContext.context.sendActivity({
      type: ActivityTypes.Trace,
      timestamp: new Date(),
      text: 'ActivityRouterDialog.processActivity()',
      label: `Got activityType: ${stepContext.context.activity.type}`
    });

    if (stepContext.context.activity.type === ActivityTypes.Event) {
      return this.onEventActivity(stepContext);
    } else {
      // We didn't get an activity type we can handle.
      await stepContext.context.sendActivity(
                `Unrecognized ActivityType: "${stepContext.context.activity.type}".`,
                undefined,
                InputHints.IgnoringInput
      );
      return { status: DialogTurnStatus.complete };
    }
  }

  /**
   * This method performs different tasks based on event name.
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async onEventActivity (stepContext) {
    const activity = stepContext.context.activity;
    await stepContext.context.sendActivity({
      type: ActivityTypes.Trace,
      timestamp: new Date(),
      text: 'ActivityRouterDialog.onEventActivity()',
      label: `Name: ${activity.name}, Value: ${JSON.stringify(activity.value)}`
    });

    // Resolve what to execute based on the event name.
    switch (activity.name) {
      case CARDS_DIALOG:
      case PROACTIVE_DIALOG:
      case ATTACHMENT_DIALOG:
      case AUTH_DIALOG:
      case SSO_DIALOG:
      case FILE_UPLOAD_DIALOG:
      case DELETE_DIALOG:
      case UPDATE_DIALOG:
        return stepContext.beginDialog(activity.name);

      case ECHO_DIALOG: {
        // Start the EchoSkillBot
        const messageActivity = MessageFactory.text("I'm the echo skill bot");
        messageActivity.deliveryMode = activity.deliveryMode;
        return stepContext.beginDialog(activity.name, { activity: messageActivity });
      }

      default:
        // We didn't get an event name we can handle.
        await stepContext.context.sendActivity(
                `Unrecognized EventName: "${stepContext.context.activity.name}".`,
                undefined,
                InputHints.IgnoringInput
        );
        return { status: DialogTurnStatus.complete };
    }
  }
}

module.exports.ActivityRouterDialog = ActivityRouterDialog;
