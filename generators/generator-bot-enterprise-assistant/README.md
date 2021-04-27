# @microsoft/generator-bot-enterprise-assistant [![NPM version][npm-image]][npm-url]

This template creates an enterprise assistant, comprised of a root bot based on the Basic Assistant template, and pre-configured with the Calendar and People templates as skills.

Includes support for:

- Greeting new and returning users
- Asking for help
- Cancelling a dialog
- Submitting feedback about the bot
- Error handling in conversations
- Repeat the previous question
- Chit chat with QnA Maker ([professional personality](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/how-to/chit-chat-knowledge-base?tabs=v1))
- Disambiguation of NLP results
- Calendar and People skills configured out of the box

## What this template is for

Use this template if you want to...

- Create a conversational experience for common enterprise scenarios (i.e. calendar management and user directory lookup)
- See examples of more complex conversational flows, and more advanced language understanding and generation
- See an example of a root bot and skills handling complex user interactions including interruptions and Adaptive Cards

## Packages

Your bot can use the [Azure Bot Framework component model](https://aka.ms/ComponentTemplateDocumentation) to extend the base functionality. From Composer, use the Package Manager to discover additional packages you can add to your bot.

This bot starts with the following packages:

- [Help and Cancel intent handling](https://www.nuget.org/packages/Microsoft.Bot.Components.HelpAndCancel/)
- [Orchestrator](https://www.nuget.org/packages/Microsoft.Bot.Builder.AI.Orchestrator/)

## Supported Languages

- English (en-US)

## Azure Resource Deployment

To run this bot you'll need the resources listed below. Create a publishing profile in Composer to provision and publish to your Azure resources for your bot.

- Azure Bot Registration configured with Microsoft Azure Active Directory authentication with access to the following scopes:
    - Calendars.ReadWrite
    - Contacts.Read
    - Directory.Read.All
    - People.Read
    - People.Read.All
    - User.ReadBasic.All
    - User.Read.All
- [Language Understanding (LUIS)][luis], or another recognizer of your choice
- [QnA Maker](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/overview/overview)
- A storage solution for persistent state storage like Azure Cosmos DB

## Using this template

### From Composer

From Composer you'll use the **New** button on the **Home** screen to create a new bot. After creation, Composer will guide you through making customizations to your bot. If you'd like to extend your bot with code, you can open up your bot using your favorite IDE (like Visual Studio) from the location you choose during the creation flow.

### From the command-line

This template can also be installed from the [command line](https://github.com/microsoft/botframework-components/blob/main/generators/command-line-instructions).

## License

[MIT License](https://github.com/microsoft/botframework-components/blob/main/LICENSE)

[luis]: https://docs.microsoft.com/en-us/azure/cognitive-services/luis/what-is-luis
[npm-image]: https://badge.fury.io/js/%40microsoft%2Fgenerator-enterprise-assistant.svg
[npm-url]: https://www.npmjs.com/package/@microsoft/generator-bot-enterprise-assistant
