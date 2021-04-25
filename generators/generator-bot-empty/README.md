# @microsoft/generator-bot-empty [![NPM version][npm-image]][npm-url]

This template creates a bot containing only a root dialog and a initial greeting dialog.

## What this template is for

Use this template if you want to...

- Start from scratch, without any extraneous files to delete.
- Build your bot by composing an ad-hoc set of packages.

## Packages

Your bot can use the [Azure Bot Framework component model](https://aka.ms/ComponentTemplateDocumentation) to extend the base functionality. From Composer, use the Package Manager to discover additional packages you can add to your bot.

## Supported Languages

- English (en-us)

## Azure Resource Deployment

This template does not rely on any additional Azure Resources.

## Using this template

### From Composer

From Composer you'll use the **New** button on the **Home** screen to create a new bot. After creation, Composer will guide you through making customizations to your bot. If you'd like to extend your bot with code, you can open up your bot using your favorite IDE (like Visual Studio) from the location you choose during the creation flow.

### From the command-line

This template can also be used from the command-line. First, install [Yeoman][yeoman] and @microsoft/generator-bot-empty using [npm][npm] (we assume you have pre-installed [node.js][nodejs]):

```bash
npm install -g yo
npm install -g @microsoft/generator-bot-empty
```

Then generate your new project:

```bash
yo @microsoft/generator-bot-empty -botname '{BOT_NAME}' -platform '{dotnet|js}' -integration '{functions|webapp}'
```

## License

[MIT License][license]

[composer]: https://github.com/microsoft/botframework-composer
[yeoman]: https://yeoman.io
[npm]: https://npmjs.com
[nodejs]: https://nodejs.org/
[license]: https://github.com/microsoft/botframework-components/blob/main/LICENSE

[npm-image]: https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-empty.svg
[npm-url]: https://www.npmjs.com/package/@microsoft/generator-bot-empty