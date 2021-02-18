// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Orchestrator;
using Microsoft.Bot.Builder.Runtime.Plugins;

namespace Microsoft.Bot.Components.Recognizers.Orchestrator
{
    public class BotPlugin : IBotPlugin
    {
        public void Load(IBotPluginLoadContext context)
        {
            ComponentRegistration.Add(new OrchestratorComponentRegistration());
        }
    }
}