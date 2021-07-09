// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const dotenv = require('dotenv');
const http = require('http');
const https = require('https');
const path = require('path');
const restify = require('restify');

// Import required bot configuration.
const ENV_FILE = path.join(__dirname, '.env');
dotenv.config({ path: ENV_FILE });

// Import required bot services.
// See https://aka.ms/bot-services to learn more about the different parts of a bot.
const { ActivityTypes, BotFrameworkAdapter, InputHints, MemoryStorage, ConversationState, SkillHttpClient, SkillHandler, ChannelServiceRoutes, TurnContext, MessageFactory, SkillConversationIdFactory } = require('botbuilder');
const { AuthenticationConfiguration, SimpleCredentialProvider } = require('botframework-connector');

const { SkillBot } = require('./bots/skillBot');
const { ActivityRouterDialog } = require('./dialogs/activityRouterDialog');
const { allowedCallersClaimsValidator } = require('./authentication/allowedCallersClaimsValidator');
const { SsoSaveStateMiddleware } = require('./middleware/ssoSaveStateMiddleware');

// Create HTTP server
const server = restify.createServer({ maxParamLength: 1000 });
server.use(restify.plugins.queryParser());

server.listen(process.env.port || process.env.PORT || 36420, () => {
  console.log(`\n${server.name} listening to ${server.url}`);
  console.log('\nGet Bot Framework Emulator: https://aka.ms/botframework-emulator');
  console.log('\nTo talk to your bot, open the emulator select "Open Bot"');
});

const maxTotalSockets = (preallocatedSnatPorts, procCount = 1, weight = 0.5, overcommit = 1.1) =>
  Math.min(
    Math.floor((preallocatedSnatPorts / procCount) * weight * overcommit),
    preallocatedSnatPorts
  );

const authConfig = new AuthenticationConfiguration([], allowedCallersClaimsValidator);

// Create adapter.
// See https://aka.ms/about-bot-adapter to learn more about how bots work.
const adapter = new BotFrameworkAdapter({
  appId: process.env.MicrosoftAppId,
  appPassword: process.env.MicrosoftAppPassword,
  authConfig: authConfig,
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
  // NOTE: In production environment, you should consider logging this to Azure application insights.
  console.error(`\n [onTurnError] unhandled error: ${error}`);

  try {
    const { message, stack } = error;

    // Send a message to the user.
    let errorMessageText = 'The skill encountered an error or bug.';
    let errorMessage = MessageFactory.text(`${errorMessageText}\r\n${message}\r\n${stack}`, errorMessageText, InputHints.IgnoringInput);
    errorMessage.value = { message, stack };
    await context.sendActivity(errorMessage);

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

    // Send and EndOfConversation activity to the skill caller with the error to end the conversation
    // and let the caller decide what to do.
    await context.sendActivity({
      type: ActivityTypes.EndOfConversation,
      code: 'SkillError',
      text: error
    });
  } catch (err) {
    console.error(`\n [onTurnError] Exception caught in onTurnError : ${err}`);
  }
};

const continuationParametersStore = {};

// Define the state store for your bot.
// See https://aka.ms/about-bot-state to learn more about using MemoryStorage.
// A bot requires a state storage system to persist the dialog and user state between messages.
const memoryStorage = new MemoryStorage();

// Create conversation and user state with in-memory storage provider.
const conversationState = new ConversationState(memoryStorage);

adapter.use(new SsoSaveStateMiddleware(conversationState));

// Create the conversationIdFactory
const conversationIdFactory = new SkillConversationIdFactory(memoryStorage);

// Create the credential provider;
const credentialProvider = new SimpleCredentialProvider(process.env.MicrosoftAppId, process.env.MicrosoftAppPassword);

// Create the skill client
const skillClient = new SkillHttpClient(credentialProvider, conversationIdFactory);

// Create the main dialog.
const dialog = new ActivityRouterDialog(server.url, conversationState, conversationIdFactory, skillClient, continuationParametersStore);

// Create the bot that will handle incoming messages.
const bot = new SkillBot(conversationState, dialog, server.url);

// Expose the manifest
server.get('/manifests/*', restify.plugins.serveStatic({ directory: './manifests', appendRequestPath: false }));

// Expose images
server.get('/images/*', restify.plugins.serveStatic({ directory: './images', appendRequestPath: false }));

// Listen for incoming requests.
server.post('/api/messages', (req, res) => {
  adapter.processActivity(req, res, async (context) => {
    // Route to main dialog.
    await bot.run(context);
  });
});

// Create and initialize the skill classes.

// Workaround for communicating back to the Host without throwing Unauthorized error due to the creation of a new Connector Client in the Adapter when the continueConvesation happens.

// Uncomment this when resolved.
// const handler = new SkillHandler(adapter, bot, conversationIdFactory, credentialProvider, authConfig);
// const skillEndpoint = new ChannelServiceRoutes(handler);
// skillEndpoint.register(server, '/api/skills');

// Remove this when resolved
const handler = new SkillHandler(adapter, bot, conversationIdFactory, credentialProvider, authConfig);
server.post('/api/skills/v3/conversations/:conversationId/activities/:activityId', async (req, res) => {
  try {
    const authHeader = req.headers.authorization || req.headers.Authorization || '';
    const activity = await ChannelServiceRoutes.readActivity(req);
    const ref = await handler.conversationIdFactory.getSkillConversationReference(req.params.conversationId);
    const claimsIdentity = await handler.authenticate(authHeader);

    const response = await new Promise(resolve => {
      return adapter.continueConversation(ref.conversationReference, ref.oAuthScope, async (context) => {
        context.turnState.set(adapter.BotIdentityKey, claimsIdentity);
        context.turnState.set(adapter.SkillConversationReferenceKey, ref);

        const newActivity = TurnContext.applyConversationReference(activity, ref.conversationReference);

        if (newActivity.type === ActivityTypes.EndOfConversation) {
          await handler.conversationIdFactory.deleteConversationReference(req.params.conversationId);
          SkillHandler.applyEoCToTurnContextActivity(context, newActivity);
          resolve(await bot.run(context));
        }

        resolve(await context.sendActivity(newActivity));
      });
    });

    res.status(200);
    res.send(response);
    res.end();
  } catch (error) {
    ChannelServiceRoutes.handleError(error, res);
  }
});

// Listen for incoming requests.
server.get('/api/music', restify.plugins.serveStatic({ directory: 'dialogs/cards/files', file: 'music.mp3' }));

// Listen for incoming notifications and send proactive messages to users.
server.get('/api/notify', async (req, res) => {
  let error;
  const { user } = req.query;

  const continuationParameters = continuationParametersStore[user];

  if (!continuationParameters) {
    res.setHeader('Content-Type', 'text/html');
    res.writeHead(200);
    res.write(`<html><body><h1>No messages sent</h1> <br/>There are no conversations registered to receive proactive messages for ${user}.</body></html>`);
    res.end();
    return;
  }

  try {
    adapter.continueConversation(continuationParameters.conversationReference, continuationParameters.oAuthScope, async context => {
      await context.sendActivity(`Got proactive message for user: ${user}`);
      await bot.run(context);
    });
  } catch (err) {
    error = err;
  }

  res.setHeader('Content-Type', 'text/html');
  res.writeHead(200);
  res.write(`<html><body><h1>Proactive messages have been sent</h1> <br/> Timestamp: ${new Date().toISOString()} <br /> Exception: ${error || ''}</body></html>`);
  res.end();
});
