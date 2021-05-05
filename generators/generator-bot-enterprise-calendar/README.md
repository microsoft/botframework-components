# @microsoft/generator-bot-enterprise-calendar [![NPM version][npm-image]][npm-url]

This bot project was created using the [Enterprise Calendar Bot](https://aka.ms/EnterpriseCalendarBot) template. You **must complete the configuration steps outlined below for your bot to function.**

### Recommended use

- Create a sophisticated bot that enables customers to interact with Office 365 Calendar using Microsoft Graph
- Customize and extend sophisticated example dialogs, bot logic, language understanding and bot responses

### Included capabilities

- Manage Office 365 calendars using Microsoft Graph

### Included packages

- [Microsoft Graph](https://www.nuget.org/packages/Microsoft.Bot.Components.Graph/)

The Enterprise Calendar Bot uses packages to extend its capabilities. [Learn more](https://aka.ms/ComponentTemplateDocumentation)

### Required Azure resources

- Azure Bot Service Registration configured with Microsoft Azure Active Directory authentication with access to the following scopes:
    - Calendars.ReadWrite
    - Contacts.Read
    - People.Read
    - User.ReadBasic.All
- [Azure Language Understanding (LUIS)][luis], or another recognizer of your choice

### Supported languages

- English (en-US)

### License

[MIT License](https://github.com/microsoft/botframework-components/blob/main/LICENSE)

[luis]: https://docs.microsoft.com/en-us/azure/cognitive-services/luis/what-is-luis
[npm-image]: https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-enterprise-calendar.svg
[npm-url]: https://www.npmjs.com/package/@microsoft/generator-bot-enterprise-calendar
