# @microsoft/generator-bot-core-assistant [![NPM version](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-core-assistant.svg)](https://www.npmjs.com/package/@microsoft/generator-bot-core-assistant)

**Important**: The QnA Maker service will retire on the 31st of March, 2025. A newer version of the question and answering capability is now available as part of Azure Cognitive Service for Language.
For question-and-answer capabilities within the Cognitive Services, see _question answering_. Starting 1st October, 2022 you won’t be able to create new QnA Maker resources.

**This template is being provided for support of existing customers.  New users should refrain from using this template for a new bot.**

A bot with Azure Language Understanding (LUIS) and common trigger phrases used to direct the conversation flow and help customers accomplish basic tasks.

### Recommended use

- Create a sophisticated conversational bot
- Customize and extend sophisticated dialogs, bot logic, language understanding and bot responses

### Included capabilities

- Greeting new and returning users
- Chit chat with QnA Maker ([professional personality](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/how-to/chit-chat-knowledge-base?tabs=v1))
- Asking for help
- Error handling in conversations
- Cancelling a dialog
- Get customer feedback
- Repeat the previous question
- Disambiguation when multiple intents are recognized

### Required Azure resources

- [Azure Language Understanding (LUIS)](https://docs.microsoft.com/en-us/azure/cognitive-services/luis/what-is-luis), or another recognizer of your choice
- [Azure QnA Maker](https://docs.microsoft.com/en-us/azure/cognitive-services/qnamaker/overview/overview)
- A storage solution for persistent state storage like Azure Cosmos DB

### Supported languages

- English (en-US)

### License

[MIT License](https://github.com/microsoft/botframework-components/blob/main/LICENSE)
