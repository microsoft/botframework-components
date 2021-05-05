# @microsoft/generator-bot-core-language [![NPM version][npm-image]][npm-url]

A simple bot with Azure Language Understanding (LUIS) and common trigger phrases used to direct the conversation flow.

### Recommended use

- Create a simple conversational bot with Azure Language Understanding ([LUIS][luis])
- Customize and extend example dialogs, bot logic, language understanding and bot responses
- Extend your bot with [Azure Bot Framework components](https://aka.ms/ComponentTemplateDocumentation)

### Included capabilities

- Welcoming new users
- Asking for help
- Responding to unknown language requests (unknown intents)
- Cancelling a dialog
- Use Azure Language Understanding Service (LUIS) for natural language processing

### Required Azure resources

- [Azure Language Understanding (LUIS)][luis], or another recognizer of your choice
- A storage solution for persistent state storage like Azure Cosmos DB

### Supported languages

- English (en-US)

### License

[MIT License](https://github.com/microsoft/botframework-components/blob/main/LICENSE)

[luis]: https://docs.microsoft.com/en-us/azure/cognitive-services/luis/what-is-luis
[npm-image]: https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-core-language.svg
[npm-url]: https://www.npmjs.com/package/@microsoft/generator-bot-core-language
