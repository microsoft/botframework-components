using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder.Dialog.Adaptive.Runtime.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace calendarskill
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, builder) =>
                {
                    string applicationRoot = AppDomain.CurrentDomain.BaseDirectory;
                    string settingsDirectory = "settings";

                    builder.AddBotRuntimeConfiguration(applicationRoot, settingsDirectory);

                    builder.AddCommandLine(args);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}