# @microsoft/generator-bot-adaptive [![NPM version][npm-image]][npm-url]

> Yeoman generator for creating a completely empty bot built on the Azure Bot Framework & Composer.

The Adaptive Bot generator is the base generator for creating an Azure Bot Framework bot using the Adaptive Dialog stack. This generator is designed to be used as a base for other more purpose-specific generators, and is responsible for scaffolding:

- The basic bot project (code files, root dialog, base schema) for your bot, based on the platform you choose (dotnet or JavaScript)
- The publishing environment for your bot (Azure Functions, or a Web App)

## Usage

### Creating your own templates

If you need to create your own templates, you can use this generator as a base, and extend it to meet your needs with [Yeoman generator composition](https://yeoman.io/authoring/composability.html). Learn more about creating your own templates in [our documentation](https://aka.ms/bf-create-templates).

### From the command line

First, install [Yeoman][yeoman] and @microsoft/generator-bot-adaptive using [npm][npm] (we assume you have pre-installed [node.js][nodejs].

```bash
npm install -g yo
npm install -g @microsoft/generator-bot-adaptive
```

Then generate your new project:

```bash
yo @microsoft/bot-adaptive -botname '{BOT_NAME}' -platform '{dotnet|js}' -integrations '{functions|webapp}'
```

Once your bot is generated, open your bot with **[Bot Framework Composer][composer]** to edit, manage, and publish your bot, or use your favorite IDE (like Visual Studio) to extend your bot with code.

## Supported Languages

- English (en-us)

## Resource Deployment

This template contains scaffold code for publishing your bot to either Azure Functions, or Azure Web App. You can also choose to use neither option, and publish to your own web application hosting service of choice.

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

[npm-image]: https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-adaptive.svg
[npm-url]: https://www.npmjs.com/package/@microsoft/generator-bot-adaptive
[composer]: https://github.com/microsoft/botframework-composer
[yeoman]: https://yeoman.io
[npm]: https://npmjs.com
[nodejs]: https://nodejs.org/
