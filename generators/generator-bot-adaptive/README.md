# @microsoft/generator-bot-adaptive [![NPM version][npm-image]][npm-url]

This generator is for creating an Azure Bot Framework bot using the Adaptive Dialog stack. This generator is designed to be used as a base for other more purpose-specific generators, and is responsible for scaffolding:

- The basic bot project (code files, root dialog, base schema) for your bot, based on the platform you choose (.NET or JavaScript)
- The publishing environment for your bot (Azure Functions, or a Web App)

You can also use our [generator for generating bot generators](https://github.com/microsoft/botframework-components/tree/main/generators/generator-bot-template-generator) to help you create your own generators and templates.

## Supported Languages

- English (en-US)

## Resource Deployment

This template contains scaffold code for publishing your bot to either Azure Functions, or Azure Web App. You can also choose to use neither option, and publish to your own web application hosting service of choice.

## Usage

### Creating your own templates

If you need to create your own templates, you can use this generator as a base, and extend it to meet your needs with [Yeoman generator composition](https://yeoman.io/authoring/composability.html). Learn more about creating your own templates in [our documentation](https://aka.ms/bf-create-templates).

### From the command-line

This template can also be installed from the [command line](https://github.com/microsoft/botframework-components/blob/main/generators/command-line-instructions).

## License

[MIT License](https://github.com/microsoft/botframework-components/blob/main/LICENSE)

[npm-image]: https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-adaptive.svg
[npm-url]: https://www.npmjs.com/package/@microsoft/generator-bot-adaptive