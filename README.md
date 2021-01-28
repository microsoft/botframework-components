---
title:  'Building bots from building blocks'
author: 'clearab'
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

## Adaptive Runtime

At the core of the component model is the adaptive runtime - an extensible, configurable runtime that is treated as a black box to bot developers and taken as a dependency. The runtime provides extensibility points to add additional functionality by importing packages, connection to skills, or adding your own functionality.

## Packages

Packages are bits of a bot you want to share/import like declarative dialog assets, coded dialogs, custom adapters, middleware or custom actions. They are just packages  - NuGet / npm etc based on the code-language of your bot. You'll use the Bot Framework CLI tool to merge the package's declarative contents with your bot (package management in Composer will handle this for you). They can be made up of any combination of declarative assets (.dialog, .lu, .lg, .qna files) or coded extensions (custom actions, middleware, adapters).

In addition to the packages published by the Bot Framework team, you'll be able to create and share your own packages. We plan to provide tooling to make the entire package management lifecycle as simple as possible, from discovery and inclusion, to creation and publishing. Some examples of packages include:

* Common conversational constructs like greeting, cancel, help, unknown intent.
* Bundles of custom actions for working with an API like MS Graph, Dynamics, the Power Platform or GitHub.
* Vertically aligned solutions containing a combination of custom actions and adaptive assets like human hand-off, or working with your calendar.
* Bundles of custom actions for working with specific types of data or operations, like math functions or working with dates.
* Meta-packages, that just take dependencies on a bunch of other packages to group functionality for simpler management.

## Templates

Getting started templates will be created on top of the component model. They will be built primarily by composing packages - ensuring that no matter which template you start from you'll have the flexibility to grow and develop your bot to meet your needs.

For example, the Conversational Core template will take a dependency on four packages - greeting, help, cancel, and unknown intent. This represents the base set of functionality nearly all conversational bots include. If you were to start from the empty/echo bot template, you could choose to add these packages later - either way you'd get the same set of functionality (without the need to do something like compare code samples and try and stitch them together yourself).

## Skills

Skills are separate bots you connect your bot to in order to process messages for you. The skill manifest establishes a contract other bots can follow - defining messages and events the skill can handle and any data that will be returned when the skill completes its interaction with your user.






## Old things

This repository is the home for a list of Bot Framework Skills that provide productivity features as well as some experimental capabilities that are built on top of the latest BotBuilder SDK that offers [Skills](https://docs.microsoft.com/en-us/azure/bot-service/skills-conceptual?view=azure-bot-service-4.0) functionality.

| Name | Description |  
|:------------:|------------| 
|[CalendarSkill (Preview)](https://aka.ms/bfcalendarskill) | Add calendar capabilities to your Assistant. Powered by Microsoft Graph and Google. |
|[EmailSkill (Preview)](https://aka.ms/bfemailskill) | Add email capabilities to your Assistant. Powered by Microsoft Graph and Google. |
|[ToDoSkill (Preview)](https://aka.ms/bftodoskill) | Add task management capabilities to your Assistant. Powered by Microsoft Graph. |
|[PointOfInterestSkill (Preview)](https://aka.ms/bfpoiskill) | Find points of interest and directions. Powered by Azure Maps and FourSquare. |
|[AutomotiveSkill (Preview)](https://aka.ms/bfautoskill) | Add automotive management capabilities to your Assistant |
|[BingSearchSkill (Preview)](https://aka.ms/bfbingsearchskill) | Add searching capabilities to your Assistant. Powered by Microsoft Bing. |
|[HospitalitySkill (Preview)](https://aka.ms/bfhospitalityskill) | Add hospitality capabilities to your Assistant. |
|[ITSMSkill (Preview)](https://aka.ms/bfitsmskill) | Add ticket and knowledge base related capabilities to your Assistant. Powered by ServiceNow. |
|[MusicSkill (Preview)](https://aka.ms/bfmusicskill) | Add music capabilities to your Assistant. Powered by Spotify. |
|[NewsSkill (Preview)](https://aka.ms/bfnewsskill) | Add news capabilities to your Assistant. Powered by Bing News Cognitive Services. |
|[PhoneSkill (Preview)](https://aka.ms/bfphoneskill) | Add phone capabilities to your Assistant. |
|[RestaurantBookingSkill (Preview)](https://aka.ms/bfrestaurantbookingskill) | Add hospitality capabilities to your Assistant. |
|[WeatherSkill (Preview)](https://aka.ms/bfweatherskill) | Add weather capabilities to your Assistant. Powered by AccuWeather. |

## Documentation

We document working with components [here][./docs/components-overview.md], and you can find the full documentation for the Bot Framework SDK & Composer [here](https://aka.ms/botdocs).

## Need Help?

If you have any questions please start with [Stack Overflow](https://stackoverflow.com/questions/tagged/botframework) where we're happy to help. Please use this GitHub Repos issue tracking capability to raise [issues](https://github.com/Microsoft/botframework-skills/issues/new?assignees=&labels=Type%3A+Bug&template=bug_report.md&title=) or [feature requests](https://github.com/Microsoft/botframework-skills/issues/new?assignees=&labels=Type%3A+Feature&template=feature_request.md&title=).

## Contributing


## Reporting Security Issues
Security issues and bugs should be reported privately, via email, to the Microsoft Security Response Center (MSRC) at [secure@microsoft.com](mailto:secure@microsoft.com). You should receive a response within 24 hours. If for some reason you do not, please follow up via email to ensure we received your original message. Further information, including the [MSRC PGP](https://technet.microsoft.com/en-us/security/dn606155) key, can be found in the [Security TechCenter](https://technet.microsoft.com/en-us/security/default).

## License
Copyright (c) Microsoft Corporation. All rights reserved.
