# bot-template-generator [![NPM version][npm-image]][npm-url]

Yeoman generator generating a bot template generator for use with the Microsoft Bot Framework.

## Getting started

### Starting with published version of generator-bot-template-generator

- Install: `npm install -g generator-bot-template-generator`
- Run: `yo bot-template-generator`

### Starting with local version of generator-bot-template-generator

- Navigate to the root dir of your local version of `generator-bot-template-generator` and run `npm install`
- Navigate to the directory where you want to instantiate your bot template generator and run `yo {path to your local versions of generator-bot-template-generator's ./app/index.js file}`
  - OR run npm link within the root of the `generator-bot-template-generator` directory and then navigate to where you want to instantiate your bot and run `yo bot-template-generator`

## Commands

- `yo bot-template-generator` shows a wizard for generating a new bot template generator

## What do you get?

Scaffolds out a complete bot template generator directory structure for you. Once you have generated this base structure proceed with the steps outlined in [How to populate your bot specific assets](#How-to-populate-your-bot-specific-assets).

## Yeomen generator basics

A bot template generator is a yeomen generator, you do not need to be a yeomen expert to develop this generator as all of the boilerplate is included for you! However a foundational knowledge will be useful especially if you start using Yeomen features that go beyond the standard scaffolding this tool outputs. Learn more about authoring yeomen generators [here](https://yeoman.io/authoring/).

For most scenarios you will stick to the standard scaffolding this tool outputs.

All you need to know from a yeomen perspective is that yeomen will run the bootstrapped logic outlined in the `index.js` file which will ultimately generate an instance of this template on the users machine. You will likely not need to divert from the existing logic implemented for you. This existing logic does the following:

- Calls the runtime generator and generates the runtime in the end users bot proj location
  - It passes the component package dependency names your template relies on to the runtime generator for proper end resolution of those dependencies in your resulting bot project
- Copies all the files in the `templates` directory and adds it the end users bot proj location

**Thats it! A template is just a list of dependent component packages (declared in index.js) and the glue/routing logic for those packages (bot files in the templates dir)**

For this particular empty bot template in the image above, little post generation config is needed because the tool defaults to generating an empty bot template. However more advanced templates will need some post creation config outlined in the next step.

## How to populate your bot specific assets

- in `./generators/app/index.js` there is an empty array called `packageReferences` passed to the runtime generator.
  - Add all the component packages your template relies on here (i.e. unknown intent c# package, etc. )
- in `./generators/app/templates` populate the `knowledge-base`, `language-generation` and `language-understanding` directories with the declarative assets that make up the routing logic and glue for your template
  - This is usually the routing logic to the components added in the previous step
- in `./generators/app/templates/setting/appsettings.json` replace `< botName >` with `<%= botName %>`

### Tips and tricks

Unless you are a declarative asset genius you should use Composer to create what your outputted bot template will look like and then use those assets to create your template.

- In Composer, start with the empty bot template in the creation flow.
- Add the components you wish your template to contain through the asset manager.
- Add the routing logic to those components to your bot
- Test and refine the end template experience.

With the above you will have the list of dependencies and routing logic needed to populate your bot specific assets outlined in the previous step. You would then manually copy the routing logic and list of dependencies to your bot template. **Soon we will have a 'save as template' feature in Composer, but until then this is the best path forward**.

## Testing your template

### CLI

You can test the output of your template generator by calling:

```
yo '{PATH TO GENERATORS index.js FILE}' {botName}
```

from whatever directory you want your bot instance outputted to. This will generate an instance of your template where you can visually validate file output

### Composer

You will likely want to test your template in Composer prior to publishing it. To do so run a dev environment of Composer and make a small change in `assetManager.ts` within the `instantiateRemoteTemplate()` function.

You override the `generatorName` variable with the path to your local generators `index.js` file. This will make it such that regardless of the template selected in the creation flow, it will be your template that is instantiated and opened in Composer.

After making the change, run `yarn build:server` followed by `yarn start:dev`. You will then have a local build of Composer with this override where you can validate your template end to end.

## License

[MIT License][license]

[composer]: https://github.com/microsoft/botframework-composer
[yeoman]: https://yeoman.io
[npm]: https://npmjs.com
[nodejs]: https://nodejs.org/
[license]: https://github.com/microsoft/botframework-components/blob/main/LICENSE
[npm-image]: https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-adaptive.svg
[npm-url]: https://www.npmjs.com/package/@microsoft/generator-bot-adaptive

