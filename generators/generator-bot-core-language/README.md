# @microsoft/generator-bot-core-language [![NPM version][npm-image]][npm-url]

This template creates a simple conversational bot, with triggers and dialogs for responding to help, welcome and cancel intents, and a simple response for unknown intents.

## What this template is for

Use this template if you want to...

- Create a basic conversational bot with natural language processing (NLP) with a recognizer like [LUIS][luis].
- See the basics of how language understanding, language generation, recognizers and dialogs work together.

## Languages

- English (en-us)

## Azure Resource Deployment

To run this bot you'll need to configure the default recognizer for your bot. In Composer, the default recognizer is LUIS. Additionally, you may want to consider a persistent state storage solution like Azure CosmosDB. Both can be provisioned and published to by creating a publishing profile in Composer.

## Using this template

### From Composer

From Composer you'll use the **New** button on the **Home** screen to create a new bot. After creation, Composer will guide you through making customizations to your bot. If you'd like to extend your bot with code, you can open up your bot using your favorite IDE (like Visual Studio) from the location you choose during the creation flow.

### From the command-line

This template can also be installed from the [command line](https://github.com/microsoft/botframework-components/blob/main/generators/command-line-instructions).

## License

[MIT License](https://github.com/microsoft/botframework-components/blob/main/LICENSE)
