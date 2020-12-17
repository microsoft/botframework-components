﻿using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder.Runtime.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace <%= botName %>
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
                IHostEnvironment env = hostingContext.HostingEnvironment;
                var applicationRoot = AppDomain.CurrentDomain.BaseDirectory;

                builder.AddBotRuntimeConfiguration(applicationRoot, env.IsDevelopment());
                builder.AddCommandLine(args);
            })
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
    }
}
