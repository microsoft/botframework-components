# The Component Model

The component model for building bots is our model for creating coded extensions (components), sharing components and declarative assets (`.dialog`, `.lu`, `.lg`, and `.qna` files), and connecting bots together with skills. This model allows you to create bots using a building-block approach, pulling in the functionality that you need when you need it, and allowing you to share functionality that you create with others.

## Adaptive runtime

At the core of the component model is the adaptive runtime, which wraps the SDK and exposes extension points for dynamically registering your components. The runtime abstracts away the bot hosting and scaffolding code from your application code, so you can focus on your bot's functionality. It also enables easy sharing and re-use of your bot's components and dialogs.

When you create a new bot project from Composer, the template you choose will create your initial application code on disk, and take a dependency on the runtime.

## Creating bot components

A **component** is a coded extension for a bot that uses the `BotComponent` class to register itself with the runtime. Your component can include things like:

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
- One or more `.schema` files that declares the inputs and outputs your component requires
- One or more `.uischema` files to tell Composer where to display your components

## Packages

Packages are bundles of functionality that can be shared between bots - they can include:

- Declarative assets like dialogs, QnA files, language understanding or language generation files
- Bot components like custom actions, triggers, or middleware
- Schema and uischema files
- Traditional package contents (modules, DLLs, other supporting files)

The packages themselves are NuGet or npm packages, with your runtime language determining which type of package your bot will use. You'll use Package Manager from Composer to install, update and remove packages for your bot. Declarative assets in a package will be copied and merged into your your bot project so that you can customize them to meet your needs.

### Creating packages

You can create and publish your own packages of components and declarative assets. When publishing your package to the public NuGet or npm feeds you'll need to make sure it is tagged with `msbot-component` in your package metadata if you'd like it to be listed in Composer by default.

### Using packages

From Package Manager in Composer you can add new packages, update versions of existing packages, and remove packages from your bot project.

When you install a package from Package Manager, the following happens:

1. The package dependency is added to your bot project (`nuget install <packagename>` or `npm install <packagename>`)
2. Any declarative assets in the `exported` folder in the package are copied into your bot project
3. Any schema files in the package are merged with your existing schema files using the BF CLI `dialog:merge' command
4. Any components in the package that need to be registered with the runtime are added to the `components` array in your `appsettings.json` file.

Package Manager also allows you to connect to additional package feeds (like MyGet, or a local feed) using the **Edit Feeds** button.

## Skills

Skills are separate bots you connect your bot to in order to process messages for you. The skill manifest establishes a contract other bots can follow - defining messages and events the skill can handle and any data that will be returned when the skill completes its interaction with your user.

## Learn more

1. Overview (this page)
1. [Creating bot components](/docs/extending-with-code.md)
1. [Using packages](/docs/extending-with-packages.md)
1. [Creating packages](/docs/creating-packages.md)
