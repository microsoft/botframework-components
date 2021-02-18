// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.MsGraph
{
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Builder.Runtime.Plugins;
    using Microsoft.Bot.Component.MsGraph;

    public class BotPlugin : IBotPlugin
    {
        public void Load(IBotPluginLoadContext context)
        {
            ComponentRegistration.Add(new MSGraphComponentRegistration());
        }
    }
}