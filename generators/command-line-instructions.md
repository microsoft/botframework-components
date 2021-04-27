# Command line instructions

Our templates can also be used from the command-line. First, install [Yeoman][yeoman] using [npm][npm] (we assume you have pre-installed [node.js][nodejs]):

```bash
npm install -g yo
```
Next, identify the template you would like to use from the table below:

| Name | npm | Version | Platforms | Integrations |
|:----:|:---:|:-------:|:---------:|:------------:|
| [Empty Bot](/generator-bot-empty) | [@microsoft/generator-bot-empty](https://www.npmjs.com/package/@microsoft/generator-bot-empty) | [![npm version](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-empty.svg)](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-empty) | dotnet, js | webapp, functions |
| [Core Bot with Language](/generator-bot-core-language) | [@microsoft/generator-bot-core-language](https://www.npmjs.com/package/@microsoft/generator-bot-core-language) | [![npm version](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-core-language.svg)](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-core-language) | dotnet, js | webapp, functions |
| [Core Assistant Bot](/generator-bot-core-assistant) | [@microsoft/generator-bot-core-assistant](https://www.npmjs.com/package/@microsoft/generator-bot-core-assistant) | [![npm version](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-core-assistant.svg)](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-core-assistant) | dotnet | webapp, functions |
| [Enterprise Assistant Bot](/generator-bot-enterprise-assistant) | [@microsoft/generator-bot-enterprise-assistant](https://www.npmjs.com/package/@microsoft/generator-bot-enterprise-assistant) | [![npm version](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-enterprise-assistant.svg)](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-enterprise-assistant) | dotnet | webapp, functions |
| [Enterprise Calendar Bot](/generator-bot-enterprise-calendar) | [@microsoft/generator-bot-enterprise-calendar](https://www.npmjs.com/package/@microsoft/generator-bot-enterprise-calendar) | [![npm version](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-enterprise-calendar.svg)](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-enterprise-calendar) | dotnet | webapp, functions |
| [Enterprise People Bot](/generator-bot-enterprise-people) | [@microsoft/generator-bot-enterprise-people](https://www.npmjs.com/package/@microsoft/generator-bot-enterprise-people) | [![npm version](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-enterprise-people.svg)](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-enterprise-people) | dotnet | webapp, functions |
| [Adaptive](/generator-bot-adaptive) | [@microsoft/generator-bot-adaptive](https://www.npmjs.com/package/@microsoft/generator-bot-adaptive) | [![npm version](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-adaptive.svg)](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-adaptive) | dotnet, js | webapp, functions |

Once you have identified the template you would like to use, install it using [npm][npm]. For example, to install the Empty Bot template:

```bash
npm install -g @microsoft/generator-bot-empty
```

Finally, generate your new project using [Yeoman][yeoman], taking note of the following:

- Remove `generator` from the package name, e.g. `@microsoft/generator-bot-empty` becomes `@microsoft/bot-empty`.
- `--platform` and `--integration` match one of the listed values from template's platforms and integrations.
  - `--platform` will default to **dotnet** if not specified.
  - `--integration` will default to **webapp** if not specified.

```bash
yo @microsoft/bot-empty '{BOT_NAME}' --platform '{dotnet|js}' --integration '{webapp|functions}'
```

[yeoman]: https://yeoman.io
[npm]: https://npmjs.com
[nodejs]: https://nodejs.org/
