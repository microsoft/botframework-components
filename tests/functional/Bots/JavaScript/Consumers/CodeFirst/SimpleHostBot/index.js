// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// index.js is used to setup and configure your bot

// Import required packages
const http = require('http');
const https = require('https');
const path = require('path');
const restify = require('restify');

// Import required bot services.
// See https://aka.ms/bot-services to learn more about the different parts of a bot.
const { BotFrameworkAdapter, TurnContext, ActivityTypes, ChannelServiceRoutes, ConversationState, InputHints, MemoryStorage, SkillHandler, SkillHttpClient, MessageFactory, SkillConversationIdFactory } = require('botbuilder');
const { AuthenticationConfiguration, SimpleCredentialProvider } = require('botframework-connector');

// Import required bot configuration.
const ENV_FILE = path.join(__dirname, '.env');
require('dotenv').config({ path: ENV_FILE });

// This bot's main dialog.
const { HostBot } = require('./bots/hostBot');
const { SkillsConfiguration } = require('./skillsConfiguration');
const { allowedSkillsClaimsValidator } = require('./authentication/allowedSkillsClaimsValidator');
const { SetupDialog } = require('./dialogs/setupDialog');

const maxTotalSockets = (preallocatedSnatPorts, procCount = 1, weight = 0.5, overcommit = 1.1) =>
  Math.min(
    Math.floor((preallocatedSnatPorts / procCount) * weight * overcommit),
    preallocatedSnatPorts
  );

// Create adapter.
// See https://aka.ms/about-bot-adapter to learn more about adapters.
const adapter = new BotFrameworkAdapter({
  appId: process.env.MicrosoftAppId,
  appPassword: process.env.MicrosoftAppPassword,
  authConfig: new AuthenticationConfiguration([], allowedSkillsClaimsValidator),
  clientOptions: {
    agentSettings: {
      http: new http.Agent({
        keepAlive: true,
        maxTotalSockets: maxTotalSockets(1024, 4, 0.3)
      }),
      https: new https.Agent({
        keepAlive: true,
        maxTotalSockets: maxTotalSockets(1024, 4, 0.7)
      })
    }
  }
});

// Catch-all for errors.
adapter.onTurnError = async (context, error) => {
  // This check writes out errors to console log .vs. app insights.
  // NOTE: In production environment, you should consider logging this to Azure
  //       application insights.
  console.error(`\n [onTurnError] unhandled error: ${error}`);

  try {
    const { message, stack } = error;

    // Send a message to the user.
    let errorMessageText = 'The bot encountered an error or bug.';
    let errorMessage = MessageFactory.text(errorMessageText, errorMessageText, InputHints.IgnoringInput);
    errorMessage.value = { message, stack };
    await context.sendActivity(errorMessage);

    await context.sendActivity(`Exception: ${message}`);
    await context.sendActivity(stack);

    errorMessageText = 'To continue to run this bot, please fix the bot source code.';
    errorMessage = MessageFactory.text(errorMessageText, errorMessageText, InputHints.ExpectingInput);
    await context.sendActivity(errorMessage);

    // Send a trace activity, which will be displayed in Bot Framework Emulator
    await context.sendTraceActivity(
      'OnTurnError Trace',
            `${error}`,
            'https://www.botframework.com/schemas/error',
            'TurnError'
    );
  } catch (err) {
    console.error(`\n [onTurnError] Exception caught in onTurnError : ${err}`);
  }

  try {
    // Inform the active skill that the conversation is ended so that it has
    // a chance to clean up.
    // Note: ActiveSkillPropertyName is set by the RooBot while messages are being
    // forwarded to a Skill.
    const activeSkill = await conversationState.createProperty(HostBot.ActiveSkillPropertyName).get(context);
    if (activeSkill) {
      const botId = process.env.MicrosoftAppId;

      let endOfConversation = {
        type: ActivityTypes.EndOfConversation,
        code: 'RootSkillError'
      };
      endOfConversation = TurnContext.applyConversationReference(
        endOfConversation, TurnContext.getConversationReference(context.activity), true);

      await conversationState.saveChanges(context, true);
      await skillClient.postToSkill(botId, activeSkill, skillsConfig.skillHostEndpoint, endOfConversation);
    }
  } catch (err) {
    console.error(`\n [onTurnError] Exception caught on attempting to send EndOfConversation : ${err}`);
  }

  try {
    // Clear out state
    await conversationState.delete(context);
  } catch (err) {
    console.error(`\n [onTurnError] Exception caught on attempting to Delete ConversationState : ${err}`);
  }
};

// Define a state store for your bot. See https://aka.ms/about-bot-state to learn more about using MemoryStorage.
// A bot requires a state store to persist the dialog and user state between messages.

// For local development, in-memory storage is used.
// CAUTION: The Memory Storage used here is for local bot debugging only. When the bot
// is restarted, anything stored in memory will be gone.
const memoryStorage = new MemoryStorage();
const conversationState = new ConversationState(memoryStorage);

// Create the conversationIdFactory
const conversationIdFactory = new SkillConversationIdFactory(memoryStorage);

// Load skills configuration
const skillsConfig = new SkillsConfiguration();

// Create the credential provider;
const credentialProvider = new SimpleCredentialProvider(process.env.MicrosoftAppId, process.env.MicrosoftAppPassword);

// Create the skill client
const skillClient = new SkillHttpClient(credentialProvider, conversationIdFactory);

// Create the main dialog.
const dialog = new SetupDialog(conversationState, skillsConfig);
const bot = new HostBot(dialog, conversationState, skillsConfig, skillClient);

// Create HTTP server
const server = restify.createServer();
server.listen(process.env.port || process.env.PORT || 36000, function () {
  console.log(`\n${server.name} listening to ${server.url}`);
  console.log('\nGet Bot Framework Emulator: https://aka.ms/botframework-emulator');
  console.log('\nTo talk to your bot, open the emulator select "Open Bot"');
});

// Listen for incoming activities and route them to your bot main dialog.
server.post('/api/messages', (req, res) => {
  // Route received a request to adapter for processing
  adapter.processActivity(req, res, async (turnContext) => {
    // route to bot activity handler.
    await bot.run(turnContext);
  });
});

// Create and initialize the skill classes
const authConfig = new AuthenticationConfiguration([], allowedSkillsClaimsValidator);
const handler = new SkillHandler(adapter, bot, conversationIdFactory, credentialProvider, authConfig);
const skillEndpoint = new ChannelServiceRoutes(handler);
skillEndpoint.register(server, '/api/skills');
