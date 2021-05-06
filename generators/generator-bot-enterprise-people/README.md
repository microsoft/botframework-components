# @microsoft/generator-bot-enterprise-people [![NPM version][npm-image]][npm-url]

A bot with the ability to interact with Office 365 users in an Azure Active Directory using Microsoft Graph. [Learn more](https://aka.ms/EnterprisePeopleBot)

### Recommended use

- Create a sophisticated bot that enables customers to interact with Azure Active Directory using Microsoft Graph
- Customize and extend sophisticated example dialogs, bot logic, language understanding and bot responses

### Included capabilities

- Search and interact with Office 365 users in an Azure Active Directory using Microsoft Graph

### Included packages

- [Microsoft Graph](https://www.nuget.org/packages/Microsoft.Bot.Components.Graph/)

The Enterprise People Bot uses packages to extend its capabilities. [Learn more](https://aka.ms/ComponentTemplateDocumentation)

### Required Azure resources

- Azure Bot Service Registration configured with Microsoft Azure Active Directory authentication with access to the following scopes:
    - Contacts.Read
    - Directory.Read.All
    - People.Read
    - People.Read.All
    - User.ReadBasic.All
    - User.Read.All
- [Azure Language Understanding (LUIS)][luis], or another recognizer of your choice

### Supported languages

- English (en-US)

### License

[MIT License](https://github.com/microsoft/botframework-components/blob/main/LICENSE)

[luis]: https://docs.microsoft.com/en-us/azure/cognitive-services/luis/what-is-luis
[npm-image]: https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-enterprise-people.svg
[npm-url]: https://www.npmjs.com/package/@microsoft/generator-bot-enterprise-people
