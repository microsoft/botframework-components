using System;
using System.Collections.Generic;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Runtime.Extensions;
using Microsoft.Extensions.Configuration;

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

            var skillHostEndpoint = configurationBuilder.ConfigurationBuilder.Build().GetValue<string>("SkillHostEndpoint");

            // If the skillhostEndpoint isn't set in the appsettings.json file, calculate a value and set it based on 
            // the WEBSITE_HOST value that gives the host of the running function. Only do this if it's not configured 
            // as the function may be behind a load balancer or proxy and we can't always rely on autocomputed 
            // value here.
            if (string.IsNullOrEmpty(skillHostEndpoint))
            {
                var hostname = configurationBuilder.ConfigurationBuilder.Build().GetValue<string>("WEBSITE_HOSTNAME");
                // for localhost use http rather than https
                var protocol = hostname.StartsWith("localhost") ? "http" : "https";
                var skillHostSettings = new Dictionary<string, string>
                {
                    { "SkillHostEndpoint", $"{protocol}://{hostname}/api/skills" },
                };
                configurationBuilder.ConfigurationBuilder.AddInMemoryCollection(skillHostSettings);
            }

        }
    }
}