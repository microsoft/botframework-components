---
title:  'Bot Framework Components'
---
# Bot Framework Components

This repository contains the source code for *components* published by Microsoft for bots built on the Azure Bot Framework technology stack. These components are part of the component model for building bots, which enables developers to build bots with re-usable building blocks (components). This model consists of a configurable [adaptive runtime](#adaptive-runtime), that can be extended by importing [packages](#packages) of functionality or connecting to other bots as [skills](#skills). Getting started [templates](#templates) built on this model will unify the creation experience, and eliminate the "dead-end" that can happen for some existing getting started experiences that lock you in to building a particular type of bot.

**Our goals**:

* Encourage the reuse of bot components – either connecting to a skill or importing in a package.
* Enable the free movement of bots and components across hosting options and editing canvases.
* Use industry/language-standard concepts and tools wherever possible.
* Abstract away platform concepts for developers who do not wish to use them directly.
* Enable provisioning and deployment to the necessary infrastructure based on the components included in a bot.
* Publish a suite of packages, templates, and skills bot developers can use to build their bots from.
* Publish components that demonstrate conversational design best practices.

## Documentation

We document working with components [here](/docs/overview.md), and you can find the full documentation for the Bot Framework SDK & Composer [here](https://aka.ms/botdocs).

## Index of Content

### Templates

Our [yeoman](https://yeoman.io) generators for scaffolding bot projects.

| Name         | Description |
|:------------:|-------------|
|[Empty bot](/generators/generator-microsoft-bot-empty) ) | The base empty bot |
|[Conversational Core](/generators/generator-microsoft-bot-conversational-core) | Basic conversational bot with NLP. |
|[Command list](/generators/generator-microsoft-bot-command-list) | Basic bot using regex and cards |
|[Calendar](/generators/generator-microsoft-bot-calendar) | A bot for working with Calendars |
|[Adaptive](/generators/generator-microsoft-bot-adaptive) | Used by other generators to scaffold web app or functions project |
|[Calendar Assistant](/generators/generator-microsoft-bot-calendar-assistant) | **Experimental** A bot that contains Conversational Core and Calendar, with Orchestrator |

### Packages

Bits of bots that you can add to your bot project.

| Name         |Type   | Description |
|:------------:|:------|-------------|
|[Welcome](/packages/Welcome) | Dialogs | Declarative assets supporting scenarios that welcome new and returning users. |
|[HelpAndCancel](/packages/HelpAndCancel) | Dialogs | Declarative assets supporting scenarios for "help" and "cancel" utterances. |
|[Onboarding](/packages/onboarding) | Dialogs |Declarative assets supporting a first time user experience. |
|[Calendar](/packages/Calendar) | Custom Actions |Custom actions supporting Calendar scenarios. |
|[Graph](/packages/Graph) | Custom Actions |Custom actions for work with the MS Graph API|
|[Orchestrator](Microsoft.Bot.Builder.AI.Orchestrator) | Recognizer | Plugin to register the Orchestrator recognizer with the runtime. |

### Virtual Assistant skills (Legacy)

You can find the list of Virtual Assistant skills [here](/skills/csharp/readme.md).

## Need Help?

Please use this GitHub Repositories issue tracking capability to raise [issues](https://github.com/Microsoft/botframework-components/issues/new?assignees=&labels=Type%3A+Bug&template=bug_report.md&title=) or [feature requests](https://github.com/Microsoft/botframework-components/issues/new?assignees=&labels=Type%3A+Feature&template=feature_request.md&title=).

## Contributing

We welcome contributions to this repository! Please see our [wiki](https://github.com/microsoft/botframework-components/wiki) for details on how to contribute. If you'd like to contribute a completely new package or template, please use our [community repo](https://github.com/BotBuilderCommunity/) and we can help publish them for you, or feel free to blaze your own trail and publish them independently.

## Reporting Security Issues

Security issues and bugs should be reported privately, via email, to the Microsoft Security Response Center (MSRC) at [secure@microsoft.com](mailto:secure@microsoft.com). You should receive a response within 24 hours. If for some reason you do not, please follow up via email to ensure we received your original message. Further information, including the [MSRC PGP](https://technet.microsoft.com/en-us/security/dn606155) key, can be found in the [Security TechCenter](https://technet.microsoft.com/en-us/security/default).

## License
Copyright (c) Microsoft Corporation. All rights reserved.
