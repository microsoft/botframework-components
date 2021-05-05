# @microsoft/generator-bot-enterprise-assistant [![NPM version][npm-image]][npm-url]

A bot with Azure Language Understanding (LUIS) and common trigger phrases used to direct the conversation flow to help customers accomplish common business tasks. [Learn more](https://aka.ms/EnterpriseAssistant)

### Recommended use

- Create a sophisticated bot
- Customize and extend sophisticated example dialogs, bot logic, language understanding and bot responses

### Included capabilities

- [Enterprise Calendar Bot](https://aka.ms/EnterpriseCalendarBot)
- [Enterprise People Bot](https://aka.ms/EnterprisePeopleBot)
- Greeting new and returning users
- Bot Framework Orchestrator to direct the conversation flow
- Chit chat with QnA Maker ([professional personality](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/how-to/chit-chat-knowledge-base?tabs=v1))
- Asking for help
- Disambiguation of user input (Intents)
- Cancelling a dialog
- Error handling
- Repeat previous question
- Getting customer feedback

## Included packages

- [Help and Cancel Intent Handling](https://www.nuget.org/packages/Microsoft.Bot.Components.HelpAndCancel/)
- [Bot Framework Orchestrator](https://www.nuget.org/packages/Microsoft.Bot.Builder.AI.Orchestrator/)

The Enterprise Assistant Bot uses packages to extend its capabilities. [Learn more](https://aka.ms/ComponentTemplateDocumentation).

### Required Azure resources

- Azure Bot Service Registration configured with Microsoft Azure Active Directory authentication with access to the following scopes:
    - Calendars.ReadWrite
    - Contacts.Read
    - Directory.Read.All
    - People.Read
    - People.Read.All
    - User.ReadBasic.All
    - User.Read.All
- [Azure Language Understanding (LUIS)][luis], or another recognizer of your choice
- [Azure QnA Maker](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/overview/overview)
- A storage solution for persistent state storage like Azure Cosmos DB

## Supported Languages

- English (en-US)

## License

[MIT License](https://github.com/microsoft/botframework-components/blob/main/LICENSE)

[luis]: https://docs.microsoft.com/en-us/azure/cognitive-services/luis/what-is-luis
[npm-image]: https://badge.fury.io/js/%40microsoft%2Fgenerator-enterprise-assistant.svg
[npm-url]: https://www.npmjs.com/package/@microsoft/generator-bot-enterprise-assistant
