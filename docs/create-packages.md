# Creating packages (how to)

>Note: If you are not familiar with creating [NuGet](https://nuget.org) or [npm](https://npmjs.com) packages in general, you may want to consult their documentation to ensure you understand the basics.

Packages are bits of bots that you want to reuse and/or share. They are simply standard NuGet or npm packages that contain any combination of the items listed below.

- Declarative files (.dialog, .lu, .lg, .qna)
- Bot components like
  - Custom actions and triggers
  - Middleware
  - Adapters

At a high level, the steps for creating a package are:

1. Create your dialog files (use Composer to create them).
2. Create your components (use your favorite IDE to create them).
3. Let Composer know about your package contents with schema files.
4. Register your code extensions with the runtime through the `BotComponent` class.
5. Package your files (use NuGet for C# runtime bots, and npm for bots using the JavaScript runtime).
6. Publish your package to a package feed (public, private, or local).

When your package is added to a bot from Package Manager in Composer, the following steps happen:

1. The package is installed using `nuget|npm install`.
2. Your declarative files are merged using the [Bot Frameowork CLI's](https://github.com/microsoft/botframework-cli) `dialog:merge` command.
    1. `dialog:merge` adds a copy of any dialog assets (.lu/.lg/.qna/.dialog files) in your package to the corresponding folding in the bot project.

> Note: While testing and debugging your package, you may find it useful to manually install and merge your package from the command line, rather than from Composer.

## Declarative files in packages

You can include declarative files (.dialog, .lu, .lg, or .qna) in your packages.

See the [Help and Cancel](/packages/HelpAndCancel) package for an example of a package containing a dialog.

## Components in packages

The contents of your package are essentially the same as what you would create if you were [extending your bot with code](/docs/extending-with-code.md). Just make sure you're using the `BotComponent` class to register your components.

### BotComponent

To dynamically register your action with the Adaptive Runtime, you'll need to define a `BotComponent` by inheriting from the Microsoft.Bot.Builder.BotComponent class. For example, here is how you register a simple custom action called MyCustomAction:

```c#
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MyBot
{
    public class MyBotComponent : BotComponent
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Component type
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<MyCustomAction>(MyCustomAction.Kind));
        }
    }
}
```

## Packaging your files

You'll package your components using the normal `pack` command for your package type (NuGet or npm). Be sure to include any necessary declarative files, as they are typically not included by default (for NuGet, you'd add them in either the .proj or .nuspec file).

## Publishing your package

You can publish your package to a local feed, or to a hosted feed (private or public). If you are planning to publish to NuGet or npm, and wish to make your package available from the default feeds in Package Manager in Composer, you must add the 'msbot-component' tag on your package.

You should also use one or more of the following tags based on the contents of your package.

- msbot-content
- msbot-middleware
- msbot-action
- msbot-trigger
- msbot-adapter

To test the package, the easiest thing to do is to [publish to a local feed](https://docs.microsoft.com/nuget/hosting-packages/local-feeds). After setting up the local feed, add it to the Package Manager in Composer so that your local packages can be installed in a bot.

## Example project files
- [DotNet](assets/MultiplyDialog.csproj)
- JavaScript
