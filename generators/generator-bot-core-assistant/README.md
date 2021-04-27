# @microsoft/generator-bot-core-assistant [![NPM version][npm-image]][npm-url]

This template creates an assistant-style conversational bot. Assistant-style bots typically help their users accomplish multiple different tasks, and have support for more varied conversational interactions.

Includes support for:

- Greeting new and returning users
- Asking for help
- Cancelling a dialog
- Submitting feedback about the bot
- Error handling in conversations
- Repeat the previous question
- Chit chat with QnA Maker ([professional personality](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/how-to/chit-chat-knowledge-base?tabs=v1))
- Disambiguation of NLP results

## What this template is for

Use this template if you want to...

- Create an assistant-style, or advanced conversational bot
- See examples of more complex conversational flows, and more advanced language understanding and generation

## Languages

- English (en-US)

## Azure Resource Deployment

To run this bot you'll need the resources listed below. Create a publishing profile in Composer to provision and publish to your Azure resources for your bot.

- [LUIS][luis], or another recognizer of your choice
- [QnA Maker](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/overview/overview)
- A storage solution for persistent state storage like Azure CosmosDB

## Using this template

### From Composer

From Composer you'll use the **New** button on the **Home** screen to create a new bot. After creation, Composer will guide you through making customizations to your bot. If you'd like to extend your bot with code, you can open up your bot using your favorite IDE (like Visual Studio) from the location you choose during the creation flow.

### From the command-line

This template can also be installed from the [command line](https://github.com/microsoft/botframework-components/blob/main/generators/command-line-instructions).

## License

[MIT License](https://github.com/microsoft/botframework-components/blob/main/LICENSE)

[luis]: https://docs.microsoft.com/en-us/azure/cognitive-services/luis/what-is-luis
[npm-image]: https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-core-assistant.svg
[npm-url]: https://www.npmjs.com/package/@microsoft/generator-bot-core-assistant