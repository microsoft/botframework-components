# Extending your bot with code (concept article)

Composer provides a powerful platform for modeling conversation flows, and comes with a robust set of pre-built triggers and actions you can use.
However, sometimes you'll need your bot to perform an action, respond to a trigger, work with an external API or do something for which there is no pre-built functionality.
Or you may need to do something more complex like write your own middleware, create a custom storage provider, or connect to your client using a custom adapter.
To accomplish these tasks (and more) you can create **bot components** (coded extensions for your bot).

## Anatomy of your bot project

When you create a bot in Composer all of the necessary files to build and run your bot are created on disk for you. Most of the files created are declarative files - dialog, QnA Maker, language understanding and language generation files. Your bot project also includes an `index.js` file or a `<mybot>.sln` file (depending on which language you chose) that can be opened in your favorite IDE. Your bot project takes a dependency on the **adaptive runtime** which provides the extension points you will use to create your components.

## Adaptive runtime

The adaptive runtime is an extensible, configurable runtime for your bot that runtime wraps the Bot Framework SDK, and provides extension points to add additional functionality by importing packages, connection to skills, or adding your own coded extensions as components. It includes both the set of Bot Framework features (adapters, middleware, telemetry client, etc.) and the set of language or deployment-specific technologies (web servers, configuration, etc.) necessary to run your bot.

The `BotComponent` interface in the adaptive runtime describes a single method that accepts Service Collection and Configuration instances. The Service Collection provides components access to the common set of Bot Framework things that comprise the runtime. The Configuration instance provides access to a scoped set of user-provided configurations (like connection strings or secrets).

## Creating bot components

A **component** is a coded extension for a bot that uses the `BotComponent` class to register itself with the adaptive runtime. Components are things like:

- Actions
- Triggers
- Middleware
- Adapters
- Storage providers
- Authentication providers
- Controllers/routes

Typically, your component will be made up of the following pieces:

- Your component code
- An implementation of the 'BotComponent' class to register your components with the runtime
  - You'll also need to update the `components` array in the `appsettings.json` file with your component name
- One or more `.schema` files that declares the inputs and outputs of your component (optional, depending on component type)
- One or more `.uischema` files to tell Composer where to display your components (optional, depending on component type)

## Learn More

- Create custom actions
- Create custom triggers
