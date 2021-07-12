// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

const dotenv = require('dotenv');
const http = require('http');
const https = require('https');
const path = require('path');
const restify = require('restify');

// Import required bot services.
// See https://aka.ms/bot-services to learn more about the different parts of a bot.
const { ActivityTypes, BotFrameworkAdapter, InputHints, MessageFactory } = require('botbuilder');
const { AuthenticationConfiguration } = require('botframework-connector');

// Import required bot configuration.
const ENV_FILE = path.join(__dirname, '.env');
dotenv.config({ path: ENV_FILE });

// This bot's main dialog.
const { EchoBot } = require('./bot');
const { allowedCallersClaimsValidator } = require('./authentication/allowedCallersClaimsValidator');

// Create HTTP server
const server = restify.createServer();
server.listen(process.env.port || process.env.PORT || 36400, () => {
  console.log(`\n${server.name} listening to ${server.url}`);
  console.log('\nGet Bot Framework Emulator: https://aka.ms/botframework-emulator');
  console.log('\nTo talk to your bot, open the emulator select "Open Bot"');
});

// Expose the manifest
server.get('/manifests/*', restify.plugins.serveStatic({ directory: './manifests', appendRequestPath: false }));

const maxTotalSockets = (preallocatedSnatPorts, procCount = 1, weight = 0.5, overcommit = 1.1) =>
  Math.min(
    Math.floor((preallocatedSnatPorts / procCount) * weight * overcommit),
    preallocatedSnatPorts
  );

// Create adapter.
// See https://aka.ms/about-bot-adapter to learn more about how bots work.
const adapter = new BotFrameworkAdapter({
  appId: process.env.MicrosoftAppId,
  appPassword: process.env.MicrosoftAppPassword,
  authConfig: new AuthenticationConfiguration([], allowedCallersClaimsValidator),
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

// Create the bot that will handle incoming messages.
const myBot = new EchoBot();

// Listen for incoming requests.
server.post('/api/messages', (req, res) => {
  adapter.processActivity(req, res, async (context) => {
    // Route to main dialog.
    await myBot.run(context);
  });
});
