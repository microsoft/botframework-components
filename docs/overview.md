# The Component Model

The component model for building bots is our framework for sharing and reusing bot functionality - through new bots with templates, using portions of bots in the form of packages, or as complete bots with skills.

## Table of Contents

1. [Overview](/docs/overview.md)
2. [Extending your bot using packages](/docs/extending-with-packages.md)
3. [Extending your bot with code](/docs/extending-with-code.md)
4. [Creating your own packages](/docs/creating-packages.md)
5. [Creating your own templates](/docs/creating-templates.md)

## Adaptive Runtime

At the core of the component model is the adaptive runtime - an extensible, configurable runtime for your bot that is treated as a black box to bot developers and taken as a dependency. The runtime wraps the Bot Framework SDK, and provides extensibility points to add additional functionality by importing packages, connection to skills, or adding your own coded extensions.

## Packages

Packages are bits of a bot you want to reuse. They can contain things like declarative dialog assets, coded dialogs, custom adapters, or custom actions. They are just regular NuGet or npm packages (depending on the code language for your bot). You'll use the Bot Framework CLI tool to merge the package's declarative contents with your bot (package management in Composer will handle this for you, or you could do it yourself using the Bot Framework CLI tool). They can be made up of any combination of declarative assets (.dialog, .lu, .lg, .qna files), coded extensions (custom actions, adapters), or just plain old package libraries.

## Templates

Getting started templates are created on top of the component model. They are built primarily by composing packages - ensuring that no matter which template you start from you'll have the flexibility to grow and develop your bot to meet your needs.

For example, the Conversational Core template will take a dependency on two packages - welcome and  help & cancel. It will also include a root dialog that wires up the dialogs in those packages as well as a dialog for handling unknown intents. This represents the base set of functionality nearly all conversational bots include. If you were to start from the empty/echo bot template, you could choose to add these packages later - either way you'd get the same set of functionality (without the need to do something like compare code samples and try and stitch them together yourself).

## Skills
X
Skills are separate bots you connect your bot to in order to process messages for you. The skill manifest establishes a contract other bots can follow - defining messages and events the skill can handle and any data that will be returned when the skill completes its interaction with your user.
