using System;
using System.Linq;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Integration.Runtime.Extensions;

[assembly: FunctionsStartup(typeof(<%= botName %>.Startup))]

namespace <%= botName %>
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddBotRuntime(builder.GetContext().Configuration);

            // The workaround below will be removed next SDK patch, when this bug fix gets released: https://github.com/microsoft/botbuilder-dotnet/issues/5239
            // In the meantime, we're guaranteed to have CoreAdapter registered as IBotFrameworkHttpAdapter, so look it up and register it as BotAdapter.
            RegisterCoreBotAdapter(builder.Services);
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder configurationBuilder)
        {
            string applicationRoot = configurationBuilder.GetContext().ApplicationRootPath;
            string settingsDirectory = <%- settingsDirectory %>;

            configurationBuilder.ConfigurationBuilder.AddBotRuntimeConfiguration(applicationRoot, settingsDirectory);
        }

        private static BotAdapter RegisterCoreBotAdapter(IServiceCollection services)
        {
            const string coreBotAdapterName = "CoreBotAdapter";
            var adapters = sp.GetServices<IBotFrameworkHttpAdapter>();

            var adapter = (BotAdapter)adapters.Single(a => typeof(BotAdapter).IsAssignableFrom(a.GetType()) && a.GetType().Name.Contains(coreBotAdapterName));
            services.AddSingleton(adapter);
        }
    }
}
