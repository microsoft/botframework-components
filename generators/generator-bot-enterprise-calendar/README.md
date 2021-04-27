# @microsoft/generator-bot-enterprise-calendar [![NPM version][npm-image]][npm-url]

This template creates a bot configured to manage Office 365 calendars using the Microsoft Graph API.

## What this template is for

Use this template if you want to...

- Support managing Office 365 calendars using Microsoft Graph
- Start from an advanced template including dialogs, language understanding, and language generation 

## Packages

This bot uses the [Azure Bot Framework component model](https://aka.ms/ComponentTemplateDocumentation) to extend its base functionality. The following packages come pre-installed:
- [Microsoft.Bot.Components.Graph](https://www.nuget.org/packages/Microsoft.Bot.Components.Graph/)

## Supported Languages

- English (en-US)

## Azure Resource Deployment

This template requires the following Azure resources:
- Azure Bot Registration configured with Microsoft Azure Active Directory authentication with access to the following scopes:
    - Calendars.ReadWrite
    - Contacts.Read
    - People.Read
    - User.ReadBasic.All
- [Language Understanding (LUIS)][luis] authoring resource

## Using this template

### From Composer

From Composer you'll use the **New** button on the **Home** screen to create a new bot. After creation, Composer will guide you through setting up your bot. If you'd like to extend your bot with code, you can open up your bot using your favorite IDE (like Visual Studio) from the location you choose during the creation flow.

### From the command-line

This template can also be installed from the [command line](https://github.com/microsoft/botframework-components/blob/main/generators/command-line-instructions).

## License

[MIT License](https://github.com/microsoft/botframework-components/blob/main/LICENSE)

[luis]: https://docs.microsoft.com/en-us/azure/cognitive-services/luis/what-is-luis
[npm-image]: https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-enterprise-calendar.svg
[npm-url]: https://www.npmjs.com/package/@microsoft/generator-bot-enterprise-calendar
