// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Orchestrator;
using Microsoft.Bot.Builder.Runtime.Plugins;

namespace Microsoft.Bot.Components.Orchestrator
{
    public class PirateBotComponent : BotComponent
    {
        public override void ConfigureServices(IServiceCollection services, IConfiguration componentConfiguration, ILogger logger)
        {
            // Component type
            services.AddSingleton<DeclarativeType>(sp => new DeclarativeType<PirateEcho>(PirateEcho.Kind));

            // Custom converter with default constructor
            services.AddSingleton<JsonConverterFactory, JsonConverterFactory<PirateEchoConverter>>();
        }
    }
}