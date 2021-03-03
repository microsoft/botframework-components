# Extending your bot with code

> Note: You should be familiar with the existing documentation on [custom actions](https://docs.microsoft.com/en-us/composer/how-to-add-custom-action) and [declarative dialogs](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-dialogs-declarative?view=azure-bot-service-4.0). This page is supplemental to those existing resources.

Extending a bot built on the new Adaptive Runtime with code is slightly different than with the current runtime available in Composer. The Adaptive Runtime is designed to be treated as a black-box dependency that can dynamically register your components, so you'll never modify it directly. Instead, you'll use the new `IPlugin` interface to let the runtime know at compile-time there are new components to include.

First, you'll create your custom action's .cs and .schema files like you would with the existing runtime (follow the documentation linked above if you don't know how to do this.)

To dynamically register your action with the Adaptive Runtime, you'll need to create two additional files. A file that implements the 'IBotPlugin' interface, and which instantiates your component registration class. Examples are given below:


```c#
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Runtime.Plugins;

namespace conversational_core_1.actions
{
    public class BotPlugin : IBotPlugin
    {
        public void Load(IBotPluginLoadContext context)
        {
            ComponentRegistration.Add(new CustomComponentRegistration());
        }
    }
}
```

And the component registration class:

```c#
using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Newtonsoft.Json;

namespace conversational_core_1.actions
{
    public class CustomComponentRegistration : ComponentRegistration, IComponentDeclarativeTypes
    {
        public IEnumerable<DeclarativeType> GetDeclarativeTypes(ResourceExplorer resourceExplorer)
        {
            // Actions
            yield return new DeclarativeType<Multiply>(Multiply.Kind);

        }

        public IEnumerable<JsonConverter> GetConverters(ResourceExplorer resourceExplorer, SourceContext sourceContext)
        {
            yield break;
        }
    }
}

```

You can also take a look at the [Calendar package](./packages/Calendar) for another example.


## Docs table of contents

1. [Overview](~/docs/overview.md)
2. [Extending your bot using packages](~/docs/extending-with-packages.md)
3. [Extending your bot with code](~/docs/extending-with-code.md)
4. [Creating your own packages](~/docs/creating-packages.md)
5. [Creating your own templates](~/docs/creating-templates.md)