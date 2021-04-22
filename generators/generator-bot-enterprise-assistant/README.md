# @microsoft/generator-bot-enterprise-assistant [![NPM version][npm-image]][npm-url]

This template creates an enterprise assistant, comprised of a root bot based on the Basic Assistant template, and pre-configured with Calendar and People as skills.

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
- See an example of a root bot and skills handling complex user interactions

## Packages

Your bot can use the [Azure Bot Framework component model](https://aka.ms/ComponentTemplateDocumentation) to extend the base functionality. From Composer, use the Package Manager to discover additional packages you can add to your bot.

This bot starts with the following packages:

- [Help and Cancel intent handling](https://www.nuget.org/packages/Microsoft.Bot.Components.HelpAndCancel/)
- [Orchestrator](https://www.nuget.org/packages/Microsoft.Bot.Builder.AI.Orchestrator/)

## Supported Languages

- English (en-us)

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

First, install [Yeoman][yeoman] and @microsoft/generator-bot-enterprise-assistant using [npm][npm] (we assume you have pre-installed [node.js][nodejs]):

```bash
npm install -g yo
npm install -g @microsoft/generator-bot-enterprise-assistant
```

Then generate your new project:

```bash
yo @microsoft/bot-enterprise-assistant '{BOT_NAME}' -platform 'dotnet' -integration 'webapp'
```

## License

MIT License

Copyright (c) Microsoft Corporation.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE

[npm-image]: https://badge.fury.io/js/%40microsoft%2Fgenerator-enterprise-assistant.svg
[npm-url]: https://www.npmjs.com/package/@microsoft/generator-bot-enterprise-assistant
[composer]: https://github.com/microsoft/botframework-composer
[yeoman]: https://yeoman.io
[npm]: https://npmjs.com
[nodejs]: https://nodejs.org/
[luis]: https://docs.microsoft.com/en-us/azure/cognitive-services/luis/what-is-luis
