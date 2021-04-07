using System;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Runtime.Extensions;

[assembly: FunctionsStartup(typeof(<%= botName %>.Startup))]

namespace <%= botName %>
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddBotRuntime(builder.GetContext().Configuration);
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder configurationBuilder)
        {
            FunctionsHostBuilderContext context = configurationBuilder.GetContext();

            string applicationRoot = context.ApplicationRootPath;
            string environmentName = context.EnvironmentName;
            string settingsDirectory = <%- settingsDirectory %>;

            configurationBuilder.ConfigurationBuilder.AddBotRuntimeConfiguration(
                applicationRoot,
                settingsDirectory,
                environmentName);
        }
    }
}