# @microsoft/generator-bot-adaptive [![NPM version][npm-image]][npm-url]

This generator is a base for creating an Azure Bot Framework bot using the Adaptive Dialog stack. This generator is designed to be used as a base for other more purpose-specific generators, and is responsible for scaffolding:

- The basic bot project (code files, root dialog, base schema) for your bot, based on the platform you choose (dotnet or JavaScript)
- The publishing environment for your bot (Azure Functions, or a Web App)

You can also use our [generator for generating bot generators](https://github.com/microsoft/botframework-components/tree/main/generators/generator-bot-template-generator) to help you create your own generators and templates.

## Usage

### Creating your own templates

If you need to create your own templates, you can use this generator as a base, and extend it to meet your needs with [Yeoman generator composition](https://yeoman.io/authoring/composability.html). Learn more about creating your own templates in [our documentation](https://aka.ms/bf-create-templates).

### From the command line

First, install [Yeoman][yeoman] and @microsoft/generator-bot-adaptive using [npm][npm] (we assume you have pre-installed [node.js][nodejs]):

```bash
npm install -g yo
npm install -g @microsoft/generator-bot-adaptive
```

Then generate your new project:

```bash
yo @microsoft/bot-adaptive -botname '{BOT_NAME}' -platform '{dotnet|js}' -integration '{functions|webapp}'
```

Once your bot is generated, open your bot with **[Bot Framework Composer][composer]** to edit, manage, and publish your bot, or use your favorite IDE (like Visual Studio) to extend your bot with code.

## Supported Languages

- English (en-us)

## Resource Deployment

This template contains scaffold code for publishing your bot to either Azure Functions, or Azure Web App. You can also choose to use neither option, and publish to your own web application hosting service of choice.

## License

[MIT License][license]

[composer]: https://github.com/microsoft/botframework-composer
[yeoman]: https://yeoman.io
[npm]: https://npmjs.com
[nodejs]: https://nodejs.org/
[license]: https://github.com/microsoft/botframework-components/blob/main/LICENSE