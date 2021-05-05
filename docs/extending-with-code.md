# Extending your bot with code (concept article)

Composer provides a powerful platform for modeling conversation flows, and comes with a robust set of pre-built triggers and actions you can use.
However, sometimes you'll need your bot to perform an action, respond to a trigger, work with an external API or do something for which there is no pre-built functionality for.
Or you may need to do something more advanced like write your own middleware, create a custom storage provider, or connect to your client using a custom adapter.
To accomplish these tasks (and more) you can create code extensions for your bot.



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


## Learn More

- Create custom actions
- Create custom triggers
