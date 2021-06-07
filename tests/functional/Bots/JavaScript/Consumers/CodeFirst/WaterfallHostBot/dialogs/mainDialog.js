// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const { ActivityTypes, DeliveryModes, InputHints, MessageFactory } = require('botbuilder');
const { ChoicePrompt, ChoiceFactory, ComponentDialog, DialogSet, SkillDialog, WaterfallDialog, DialogTurnStatus, ListStyle } = require('botbuilder-dialogs');
const { RootBot } = require('../bots/rootBot');
const { SsoDialog } = require('./sso/ssoDialog');
const { TangentDialog } = require('./tangentDialog');

const MAIN_DIALOG = 'MainDialog';
const DELIVERY_PROMPT = 'DeliveryModePrompt';
const SKILL_GROUP_PROMPT = 'SkillGroupPrompt';
const SKILL_PROMPT = 'SkillPrompt';
const SKILL_ACTION_PROMPT = 'SkillActionPrompt';
const TANGENT_DIALOG = 'TangentDialog';
const WATERFALL_DIALOG = 'WaterfallDialog';
const SSO_DIALOG_PREFIX = 'Sso';

class MainDialog extends ComponentDialog {
  /**
   * @param {import('botbuilder').ConversationState} conversationState
   * @param {import('../skillsConfiguration').SkillsConfiguration} skillsConfig
   * @param {import('botbuilder').SkillHttpClient} skillClient
   * @param {import('../skillConversationIdFactory').SkillConversationIdFactory} conversationIdFactory
   */
  constructor (conversationState, skillsConfig, skillClient, conversationIdFactory) {
    super(MAIN_DIALOG);

    const botId = process.env.MicrosoftAppId;

    if (!conversationState) throw new Error('[MainDialog]: Missing parameter \'conversationState\' is required');
    if (!skillsConfig) throw new Error('[MainDialog]: Missing parameter \'skillsConfig\' is required');
    if (!skillClient) throw new Error('[MainDialog]: Missing parameter \'skillClient\' is required');
    if (!conversationIdFactory) throw new Error('[MainDialog]: Missing parameter \'conversationIdFactory\' is required');

    this.deliveryModeProperty = conversationState.createProperty(RootBot.DeliveryModePropertyName);
    this.activeSkillProperty = conversationState.createProperty(RootBot.ActiveSkillPropertyName);
    this.skillsConfig = skillsConfig;
    this.deliveryMode = '';

    // Register the tangent dialog for testing tangents and resume.
    this.addDialog(new TangentDialog(TANGENT_DIALOG));

    // Create and add SkillDialog instances for the configured skills.
    this.addSkillDialogs(conversationState, conversationIdFactory, skillClient, skillsConfig, botId);

    // Add ChoicePrompt to render available delivery modes.
    this.addDialog(new ChoicePrompt(DELIVERY_PROMPT));

    // Add ChoicePrompt to render available groups of skills.
    this.addDialog(new ChoicePrompt(SKILL_GROUP_PROMPT));

    // Add ChoicePrompt to render available skills.
    this.addDialog(new ChoicePrompt(SKILL_PROMPT));

    // Add ChoicePrompt to render skill actions.
    this.addDialog(new ChoicePrompt(SKILL_ACTION_PROMPT));

    // Special case: register SSO dialogs for skills that support SSO actions.
    this.addSsoDialogs();

    this.addDialog(new WaterfallDialog(WATERFALL_DIALOG, [
      this.selectDeliveryModeStep.bind(this),
      this.selectSkillGroupStep.bind(this),
      this.selectSkillStep.bind(this),
      this.selectSkillActionStep.bind(this),
      this.callSkillActionStep.bind(this),
      this.finalStep.bind(this)
    ]));

    this.initialDialogId = WATERFALL_DIALOG;
  }

  /**
   * The run method handles the incoming activity (in the form of a TurnContext) and passes it through the dialog system.
   * If no dialog is active, it will start the default dialog.
   * @param {import('botbuilder').TurnContext} turnContext
   * @param {*} accessor
   */
  async run (turnContext, accessor) {
    const dialogSet = new DialogSet(accessor);
    dialogSet.add(this);

    const dialogContext = await dialogSet.createContext(turnContext);
    const results = await dialogContext.continueDialog();
    if (results.status === DialogTurnStatus.empty) {
      await dialogContext.beginDialog(this.id);
    }
  }

  /**
   * @param {import('botbuilder-dialogs').DialogContext} innerDc
   */
  async onContinueDialog (innerDc) {
    const activeSkill = await this.activeSkillProperty.get(innerDc.context, () => null);
    const activity = innerDc.context.activity;
    if (activeSkill != null && activity.type === ActivityTypes.Message && activity.text != null && activity.text.toLowerCase() === 'abort') {
      // Cancel all dialogs when the user says abort.
      // The SkillDialog automatically sends an EndOfConversation message to the skill to let the
      // skill know that it needs to end its current dialogs, too.
      await innerDc.cancelAllDialogs();
      return innerDc.replaceDialog(this.initialDialogId, { text: 'Canceled! \n\n What delivery mode would you like to use?' });
    }
    // Sample to test a tangent when in the middle of a skill conversation.
    if (activeSkill != null && activity.type === ActivityTypes.Message && activity.text != null && activity.text.toLowerCase() === 'tangent') {
      // Start tangent.
      return innerDc.beginDialog(TANGENT_DIALOG);
    }

    return super.onContinueDialog(innerDc);
  }

  /**
   * Render a prompt to select the delivery mode to use.
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async selectDeliveryModeStep (stepContext) {
    // Create the PromptOptions with the delivery modes supported.
    const messageText = stepContext.options && stepContext.options.text ? stepContext.options.text : 'What delivery mode would you like to use?';
    const retryMessageText = 'That was not a valid choice, please select a valid delivery mode.';

    return stepContext.prompt(DELIVERY_PROMPT, {
      prompt: MessageFactory.text(messageText, messageText, InputHints.ExpectingInput),
      retryPrompt: MessageFactory.text(retryMessageText, retryMessageText, InputHints.ExpectingInput),
      choices: ChoiceFactory.toChoices([DeliveryModes.Normal, DeliveryModes.ExpectReplies])
    });
  }

  /**
   * Render a prompt to select the group of skills to use.
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async selectSkillGroupStep (stepContext) {
    // Set delivery mode.
    this.deliveryMode = stepContext.result.value;
    await this.deliveryModeProperty.set(stepContext.context, stepContext.result.value);

    const messageText = 'What group of skills would you like to use?';
    const retryMessageText = 'That was not a valid choice, please select a valid skill group.';

    // Get a list of the groups for the skills in skillsConfig.
    const groups = Object.values(this.skillsConfig.skills)
      .map(skill => skill.group);
    // Remove duplicates
    const choices = [...new Set(groups)];

    // Create the PromptOptions from the skill configuration which contains the list of configured skills.
    return stepContext.prompt(SKILL_GROUP_PROMPT, {
      prompt: MessageFactory.text(messageText, messageText, InputHints.ExpectingInput),
      retryPrompt: MessageFactory.text(retryMessageText, retryMessageText, InputHints.ExpectingInput),
      choices: ChoiceFactory.toChoices(choices)
    });
  }

  /**
   * Render a prompt to select the skill to call.
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async selectSkillStep (stepContext) {
    const skillGroup = stepContext.result.value;

    // Create the PromptOptions from the skill configuration which contains the list of configured skills.
    const messageText = 'What skill would you like to call?';
    const retryMessageText = 'That was not a valid choice, please select a valid skill.';

    // Get skills for the selected group.
    const choices = Object.entries(this.skillsConfig.skills)
      .filter(([, skill]) => skill.group === skillGroup)
      .map(([id]) => id);

    return stepContext.prompt(SKILL_PROMPT, {
      prompt: MessageFactory.text(messageText, messageText, InputHints.ExpectingInput),
      retryPrompt: MessageFactory.text(retryMessageText, retryMessageText, InputHints.ExpectingInput),
      choices: ChoiceFactory.toChoices(choices),
      style: ListStyle.list
    });
  }

  /**
   * Render a prompt to select the begin action for the skill.
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async selectSkillActionStep (stepContext) {
    const selectedSkill = this.skillsConfig.skills[stepContext.result.value];
    const v3Bots = ['EchoSkillBotDotNetV3', 'EchoSkillBotJSV3'];

    // Set active skill.
    await this.activeSkillProperty.set(stepContext.context, selectedSkill);

    // Exclude v3 bots from ExpectReplies.
    if (this.deliveryMode === DeliveryModes.ExpectReplies && v3Bots.includes(selectedSkill.id)) {
      await stepContext.context.SendActivityAsync(MessageFactory.text("V3 Bots do not support 'expectReplies' delivery mode."));

      // Forget delivery mode and skill invocation.
      await this.deliveryModeProperty.delete(stepContext.context);
      await this.activeSkillProperty.delete(stepContext.context);

      // Restart setup dialog.
      return stepContext.replaceDialog(this.initialDialogId);
    }

    const skillActionChoices = selectedSkill.getActions();

    if (skillActionChoices && skillActionChoices.length === 1) {
      // The skill only supports one action (e.g. Echo), skip the prompt.
      return stepContext.next({ value: skillActionChoices[0] });
    }

    // Create the PromptOptions with the actions supported by the selected skill.
    const messageText = `Select an action to send to **${selectedSkill.id}**.`;

    return stepContext.prompt(SKILL_ACTION_PROMPT, {
      prompt: MessageFactory.text(messageText, messageText, InputHints.ExpectingInput),
      choices: ChoiceFactory.toChoices(skillActionChoices)
    });
  }

  /**
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async callSkillActionStep (stepContext) {
    const selectedSkill = await this.activeSkillProperty.get(stepContext.context);

    // Save active skill in state.
    await this.activeSkillProperty.set(stepContext.context, selectedSkill);

    // Create the initial activity to call the skill.
    const skillActivity = this.skillsConfig.skills[selectedSkill.id].createBeginActivity(stepContext.result.value);

    if (skillActivity.name === 'Sso') {
      // Special case, we start the SSO dialog to prepare the host to call the skill.
      return stepContext.beginDialog(`${SSO_DIALOG_PREFIX}${selectedSkill.id}`);
    }

    // We are manually creating the activity to send to the skill; ensure we add the ChannelData and Properties
    // from the original activity so the skill gets them.
    // Note: this is not necessary if we are just forwarding the current activity from context.
    skillActivity.channelData = stepContext.context.activity.channelData;
    skillActivity.properties = stepContext.context.activity.properties;

    // Create the BeginSkillDialogOptions and assign the activity to send.
    const skillDialogArgs = { activity: skillActivity };

    if (this.deliveryMode === DeliveryModes.ExpectReplies) {
      skillDialogArgs.activity.deliveryMode = DeliveryModes.ExpectReplies;
    }

    // Start the skillDialog instance with the arguments.
    return stepContext.beginDialog(selectedSkill.id, skillDialogArgs);
  }

  /**
   * The SkillDialog has ended, render the results (if any) and restart MainDialog.
   * @param {import('botbuilder-dialogs').WaterfallStepContext} stepContext
   */
  async finalStep (stepContext) {
    const activeSkill = await this.activeSkillProperty.get(stepContext.context);

    if (stepContext.result) {
      let message = `Skill "${activeSkill.id}" invocation complete.`;
      message += ` Result: ${JSON.SerializeObject(stepContext.result)}`;
      await stepContext.context.sendActivity(message);
    }

    // Forget delivery mode and skill invocation.
    await this.deliveryModeProperty.delete(stepContext.context);
    await this.activeSkillProperty.delete(stepContext.context);

    // Restart setup dialog
    return stepContext.replaceDialog(this.initialDialogId, { text: `Done with "${activeSkill.id}". \n\n What delivery mode would you like to use?` });
  }

  /**
   * Helper method that creates and adds SkillDialog instances for the configured skills.
   * @param {import('botbuilder').ConversationState} conversationState
   * @param {import('../skillConversationIdFactory').SkillConversationIdFactory} conversationIdFactory
   * @param {import('botbuilder').SkillHttpClient} skillClient
   * @param {import('../skillsConfiguration').SkillsConfiguration} skillsConfig
   * @param {string} botId
   */
  addSkillDialogs (conversationState, conversationIdFactory, skillClient, skillsConfig, botId) {
    Object.keys(skillsConfig.skills).forEach((skillId) => {
      const skillInfo = skillsConfig.skills[skillId];

      const skillDialogOptions = {
        botId: botId,
        conversationIdFactory,
        conversationState,
        skill: skillInfo,
        skillHostEndpoint: process.env.SkillHostEndpoint,
        skillClient
      };

      // Add a SkillDialog for the selected skill.
      this.addDialog(new SkillDialog(skillDialogOptions, skillInfo.id));
    });
  }

  /**
   * Special case.
   * SSO needs a dialog in the host to allow the user to sign in.
   * We create and several SsoDialog instances for each skill that supports SSO.
   */
  addSsoDialogs () {
    const addDialogs = (name, connectionName) => Object.values(this.dialogs.dialogs)
      .filter(({ id }) => id.startsWith(name))
      .forEach(skill => this.addDialog(new SsoDialog(`${SSO_DIALOG_PREFIX}${skill.id}`, skill, connectionName)));

    addDialogs('WaterfallSkillBot', process.env.SsoConnectionName);
    addDialogs('TeamsSkillBot', process.env.SsoConnectionNameTeams);
  }
}

module.exports.MainDialog = MainDialog;
