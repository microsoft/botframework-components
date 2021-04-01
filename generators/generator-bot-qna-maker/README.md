# @microsoft/generator-bot-qna-maker [![NPM version][npm-image]][npm-url]

> Yeoman generator for creating a bot and connecting to a QnA Maker knowledge base.

The QnA Maker template creates a basic bot, and guides you through connecting that bot to a QnA Maker knowledge base. You can connect to an existing knowledge base, create one from scratch using .qna files, or create one from an existing FAQ-style website.

## What this template is for

Use this template if you want to...

- Create a bot and connect it to a QnA Maker knowledge base

## Packages

Your bot can use the [Azure Bot Framework component model](https://aka.ms/ComponentTemplateDocumentation) to extend the base functionality. From Composer, use the Package Manager to discover additional packages you can add to your bot.

## Languages

- English (en-us)

## Azure Resource Deployment

To run this bot you'll need the resources listed below. Create a publishing profile in Composer to provision and publish to your Azure resources for your bot.

- [LUIS][luis], or another recognizer of your choice
- [QnA Maker](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/overview/overview)

## Using this template

### From Composer

From Composer you'll use the **New** button on the **Home** screen to create a new bot. After creation, Composer will guide you through making customizations to your bot. If you'd like to extend your bot with code, you can open up your bot using your favorite IDE (like Visual Studio) from the location you choose during the creation flow.

### From the command-line

> You can instantiate this template from the command line, however this approach is NOT recommended, as Composer guides you through connecting to your QnA Maker knowledge base.

First, install [Yeoman][yeoman] and @microsoft/generator-bot-qna-maker using [npm][npm] (we assume you have pre-installed [node.js][nodejs]):

```bash
npm install -g yo
npm install -g @microsoft/generator-bot-qna-maker
```

Then generate your new project:

```bash
yo @microsoft/bot-conversational-qna-maker '{BOT_NAME}' -platform '{dotnet|js}' -integration '{functions|webapp}'
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

[npm-image]: https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-qna-maker.svg
[npm-url]: https://www.npmjs.com/package/@microsoft/generator-bot-qna-maker
[composer]: https://github.com/microsoft/botframework-composer
[yeoman]: https://yeoman.io
[npm]: https://npmjs.com
[nodejs]: https://nodejs.org/
[luis]: https://docs.microsoft.com/en-us/azure/cognitive-services/luis/what-is-luis
