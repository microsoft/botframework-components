# @microsoft/generator-bot-calendar [![NPM version][npm-image]][npm-url]
> Yeoman generator for creating an Adaptive bot built on the Azure Bot Framework using the Calendar template.

## Installation

First, install [Yeoman](http://yeoman.io) and @microsoft/generator-bot-calendar using [npm](https://www.npmjs.com/) (we assume you have pre-installed [node.js](https://nodejs.org/)):

```bash
npm install -g yo
npm install -g @microsoft/generator-bot-calendar
```

Then generate your new project:

```bash
yo @microsoft/bot-calendar '{BOT_NAME}'
```

## Packages
Adaptive bots can utilize the [Azure Bot Framework component model](https://aka.ms/ComponentTemplateDocumentation) to extend their base functionality. The following component packages are included:

- Microsoft.Bot.Components.Graph

## Languages
English.

## Azure Resource Deployment
This template requires Microsoft Azure Active Directory authentication to be configured on your Azure Bot Service resource with access to the following scopes:
- Calendars.ReadWrite
- Contacts.Read
- People.Read
- User.ReadBasic.All

## Getting To Know Yeoman

 * Yeoman has a heart of gold.
 * Yeoman is a person with feelings and opinions, but is very easy to work with.
 * Yeoman can be too opinionated at times but is easily convinced not to be.
 * Feel free to [learn more about Yeoman](http://yeoman.io/).

## License
Copyright (c) Microsoft Corporation. All rights reserved.

[npm-image]: https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-calendar.svg
[npm-url]: https://www.npmjs.com/package/@microsoft/generator-bot-calendar
