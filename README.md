# Bot Framework Components

This repository contains *components* published by Microsoft for bots built on the Azure Bot Framework technology stack. They are part of the component model for building bots with re-usable building blocks. The model is built on a configurable [adaptive runtime](#adaptive-runtime), that can be extended by adding your own code, importing [packages](#packages) of functionality or connecting to other bots as [skills](#skills). Getting started [templates](#templates) provide dynamic code scaffolding, helping users get started quickly based on their scenario.

## Using Components

You'll primarily use components through [**Bot Framework Composer**](https://github.com/microsoft/BotFramework-Composer) - our visual bot authoring canvas for developers. From Composer you can add and remove packages from your bot, and the creation process creates bots built from the templates here.

## Creating your own components

You can also create your own packages and templates for use from Composer. We document creating components [here](https://docs.microsoft.com/composer/concept-packages). You can also check out [our samples](https://github.com/microsoft/BotBuilder-Samples/tree/main/composer_samples).

## Index of Content

### Templates

Templates are pre-built bot projects designed for specific scenarios. We use [yeoman](https://yeoman.io) generators for scaffolding our templates.

| Name         | npm | Description |
|:------------:|:---:|:------------|
|[Empty Bot](/generators/generator-bot-empty) | [![npm version](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-empty.svg)](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-empty) | A simple bot with a root dialog and greeting dialog. |
|[Core Bot with Azure Language Understanding](/generators/generator-bot-core-language) | [![npm version](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-core-language.svg)](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-core-language) | A simple bot with Azure Language Understanding (LUIS) and common trigger phrases used to direct the conversation flow. |
|[Core Assistant Bot](/generators/generator-bot-core-assistant) | [![npm version](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-core-assistant.svg)](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-core-assistant) | A bot with Azure Language Understanding (LUIS) and common trigger phrases used to direct the conversation flow and help customers accomplish basic tasks. Designed to be extended with skills. |
|[Enterprise Assistant Bot](/generators/generator-bot-enterprise-assistant) | [![npm version](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-enterprise-assistant.svg)](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-enterprise-assistant) | A Core Assistant Bot with Calendar & People as skills. |
|[Enterprise Calendar Bot](/generators/generator-bot-enterprise-calendar) | [![npm version](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-enterprise-calendar.svg)](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-enterprise-calendar) | A bot with the ability to interact with M365 Calendar using Microsoft Graph. |
|[Enterprise People Bot](/generators/generator-bot-enterprise-people) | [![npm version](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-enterprise-people.svg)](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-enterprise-people) | A bot with the ability to search for people within Azure Active Directory using Microsoft Graph.|
|[Adaptive Bot Generator](/generators/generator-bot-adaptive) | [![npm version](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-adaptive.svg)](https://badge.fury.io/js/%40microsoft%2Fgenerator-bot-adaptive) | Used by other generators to scaffold web app or functions project. |

### Packages

Packages are bits of bots that you can add to your bot project. They can contain coded extensions like custom actions, adapters, or triggers, and declarative assets like dialogs, language generation or language understanding files.

| Name         |Type   | NuGet | npm |Description |
|:------------:|:------|:-----:|:---:|:-----------|
|[Welcome](/packages/Welcome) | Dialogs | [![NuGet Badge](https://buildstats.info/nuget/Microsoft.Bot.Components.Welcome?includePreReleases=true)](https://www.nuget.org/packages/Microsoft.Bot.Components.Welcome/)| [![npm version](https://badge.fury.io/js/%40microsoft%2Fbot-components-welcome.svg)](https://badge.fury.io/js/%40microsoft%2Fbot-components-welcome) | Declarative assets supporting scenarios that welcome new and returning users. |
|[HelpAndCancel](/packages/HelpAndCancel) | Dialogs | [![NuGet Badge](https://buildstats.info/nuget/Microsoft.Bot.Components.HelpAndCancel?includePreReleases=true)](https://www.nuget.org/packages/Microsoft.Bot.Components.HelpAndCancel/) | [![npm version](https://badge.fury.io/js/%40microsoft%2Fbot-components-helpandcancel.svg)](https://badge.fury.io/js/%40microsoft%2Fbot-components-helpandcancel) | Declarative assets supporting scenarios for "help" and "cancel" utterances. |
|[Graph](/packages/Graph) | Custom Actions | [![NuGet Badge](https://buildstats.info/nuget/Microsoft.Bot.Components.Graph?includePreReleases=true)](https://www.nuget.org/packages/Microsoft.Bot.Components.Graph/) | | Custom actions for working with calendars and people through the MS Graph API.|
|[Teams](/packages/Teams) | Triggers Actions | [![NuGet Badge](https://buildstats.info/nuget/Microsoft.Bot.Components.Teams?includePreReleases=true)](https://www.nuget.org/packages/Microsoft.Bot.Components.Teams/) | | Triggers and actions for working with Microsoft Teams.|

### Virtual Assistant skills (Legacy)

Skills built to work with the [Virtual Assistant](https://docs.microsoft.com/azure/bot-service/bot-builder-virtual-assistant-introduction) template. You can find the list of Virtual Assistant skills [here](/skills/csharp/readme.md).

## Need Help?

Please use this GitHub repository issue to raise any [issues](https://github.com/Microsoft/botframework-components/issues/new?assignees=&labels=Type%3A+Bug&template=bug_report.md&title=) you encounter consuming these components, or [feature requests](https://github.com/Microsoft/botframework-components/issues/new?assignees=&labels=Type%3A+Feature&template=feature_request.md&title=) you'd like to see added.

## Contributing

We welcome contributions to this repository! Please see our [wiki](https://github.com/microsoft/botframework-components/wiki) for details on how to contribute.

If you'd like to contribute a completely new package or template, please use our [community repo](https://github.com/BotBuilderCommunity/) and we can help publish them for you, or feel free to blaze your own trail and publish them independently.

## Reporting Security Issues

Security issues and bugs should be reported privately, via email, to the Microsoft Security Response Center (MSRC) at [secure@microsoft.com](mailto:secure@microsoft.com). You should receive a response within 24 hours. If for some reason you do not, please follow up via email to ensure we received your original message. Further information, including the [MSRC PGP](https://technet.microsoft.com/security/dn606155) key, can be found in the [Security TechCenter](https://technet.microsoft.com/security/default).

## License

MIT License

Copyright (c) Microsoft Corporation.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.