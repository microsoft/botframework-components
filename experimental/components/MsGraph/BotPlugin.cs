// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Components.MsGraph
{
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Runtime.Plugins;
    using Microsoft.BotFramework.Composer.CustomAction;

    public class BotPlugin : IBotPlugin
    {
        public void Load(IBotPluginLoadContext context)
        {
            ComponentRegistration.Add(new MSGraphComponentRegistration());
        }
    }
}