# @microsoft/generator-bot-enterprise-people [![NPM version][npm-image]][npm-url]

This template creates a bot configured to search for users in an Azure Active Directory using Microsoft Graph.

## What this template is for

Use this template if you want to...

- Support searching for Azure Active Directory users
- Start from an advanced template including dialogs, language understanding, and language generation 

## Packages

This bot uses the [Azure Bot Framework component model](https://aka.ms/ComponentTemplateDocumentation) to extend its base functionality. The following packages come pre-installed:
- [Microsoft.Bot.Components.Graph](https://www.nuget.org/packages/Microsoft.Bot.Components.Graph/)

## Supported Languages

- English (en-us)

## Azure Resource Deployment

This template requires the following Azure resources:
- Azure Bot Registration configured with Microsoft Azure Active Directory authentication with access to the following scopes:
    - Contacts.Read
    - Directory.Read.All
    - People.Read
    - People.Read.All
    - User.ReadBasic.All
    - User.Read.All
- Language Understanding (LUIS) authoring resource

## Using this template

### From Composer

From Composer you'll use the **New** button on the **Home** screen to create a new bot. After creation, Composer will guide you through setting up your bot. If you'd like to extend your bot with code, you can open up your bot using your favorite IDE (like Visual Studio) from the location you choose during the creation flow.

### From the command-line

This template can also be installed from the [command line](https://github.com/microsoft/botframework-components/blob/main/generators/command-line-instructions).

## License

[MIT License](https://github.com/microsoft/botframework-components/blob/main/LICENSE)
