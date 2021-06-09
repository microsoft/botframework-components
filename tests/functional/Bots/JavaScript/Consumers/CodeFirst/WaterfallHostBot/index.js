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
const { BotFrameworkAdapter, TurnContext, ActivityTypes, ChannelServiceRoutes, ConversationState, InputHints, MemoryStorage, SkillHttpClient, MessageFactory, SkillConversationIdFactory } = require('botbuilder');
const { AuthenticationConfiguration, SimpleCredentialProvider } = require('botframework-connector');

// Import required bot configuration.
const ENV_FILE = path.join(__dirname, '.env');
require('dotenv').config({ path: ENV_FILE });

// This bot's main dialog.
const { RootBot } = require('./bots/rootBot');
const { SkillsConfiguration } = require('./skillsConfiguration');
const { allowedSkillsClaimsValidator } = require('./authentication/allowedSkillsClaimsValidator');
const { MainDialog } = require('./dialogs/mainDialog');
const { LoggerMiddleware } = require('./middleware/loggerMiddleware');
const { TokenExchangeSkillHandler } = require('./TokenExchangeSkillHandler');

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

// Use the logger middleware to log messages. The default logger argument for LoggerMiddleware is Node's console.log().
adapter.use(new LoggerMiddleware());

// Catch-all for errors.
const onTurnErrorHandler = async (context, error) => {
  // This check writes out errors to the console log, instead of to app insights.
  // NOTE: In production environment, you should consider logging this to Azure
  //       application insights. See https://aka.ms/bottelemetry for telemetry
  //       configuration instructions.
  console.error(`\n [onTurnError] unhandled error: ${error}`);

  await sendErrorMessage(context, error);
  await endSkillConversation(context);
  await clearConversationState(context);
};

async function sendErrorMessage (context, error) {
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

    // Send a trace activity, which will be displayed in Bot Framework Emulator.
    await context.sendTraceActivity(
      'OnTurnError Trace',
            `${error}`,
            'https://www.botframework.com/schemas/error',
            'TurnError'
    );
  } catch (err) {
    console.error(`\n [onTurnError] Exception caught in sendErrorMessage: ${err}`);
  }
}

async function endSkillConversation (context) {
  try {
    // Inform the active skill that the conversation is ended so that it has
    // a chance to clean up.
    // Note: ActiveSkillPropertyName is set by the RooBot while messages are being
    // forwarded to a Skill.
    const activeSkill = await conversationState.createProperty(RootBot.ActiveSkillPropertyName).get(context);
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
}

async function clearConversationState (context) {
  try {
    // Delete the conversationState for the current conversation to prevent the
    // bot from getting stuck in a error-loop caused by being in a bad state.
    // ConversationState should be thought of as similar to "cookie-state" in a Web page.
    await conversationState.delete(context);
  } catch (err) {
    console.error(`\n [onTurnError] Exception caught on attempting to Delete ConversationState : ${err}`);
  }
}

// Set the onTurnError for the singleton BotFrameworkAdapter.
adapter.onTurnError = onTurnErrorHandler;

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
const mainDialog = new MainDialog(conversationState, skillsConfig, skillClient, conversationIdFactory);
const bot = new RootBot(conversationState, skillClient, mainDialog);

// Create HTTP server.
// maxParamLength defaults to 100, which is too short for the conversationId created in skillConversationIdFactory.
// See: https://github.com/microsoft/BotBuilder-Samples/issues/2194.
const server = restify.createServer({ maxParamLength: 1000 });
server.listen(process.env.port || process.env.PORT || 36020, function () {
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
const handler = new TokenExchangeSkillHandler(adapter, bot, conversationIdFactory, skillsConfig, skillClient, credentialProvider, authConfig);
const skillEndpoint = new ChannelServiceRoutes(handler);
skillEndpoint.register(server, '/api/skills');

// Listen for Upgrade requests for Streaming.
server.on('upgrade', (req, socket, head) => {
  // Create an adapter scoped to this WebSocket connection to allow storing session data.
  const streamingAdapter = new BotFrameworkAdapter({
    appId: process.env.MicrosoftAppId,
    appPassword: process.env.MicrosoftAppPassword
  });
    // Set onTurnError for the BotFrameworkAdapter created for each connection.
  streamingAdapter.onTurnError = onTurnErrorHandler;

  streamingAdapter.useWebSocket(req, socket, head, async (context) => {
    // After connecting via WebSocket, run this logic for every request sent over
    // the WebSocket connection.
    await bot.run(context);
  });
});
