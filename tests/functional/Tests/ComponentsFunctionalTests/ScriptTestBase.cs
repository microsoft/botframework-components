// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using ComponentsFunctionalTests.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TranscriptTestRunner.TestClients;
using Xunit.Abstractions;

namespace ComponentsFunctionalTests
{
    public class ScriptTestBase
    {
        public ScriptTestBase(ITestOutputHelper output)
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.Development.json", true, true)
                .AddEnvironmentVariables()
                .Build();

            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .AddConfiguration(configuration.GetSection("Logging"))
                    .AddConsole()
                    .AddDebug()
                    .AddFile(Directory.GetCurrentDirectory() + @"/Logs/Log.json", isJson: true)
                    .AddXunit(output);
            });

            Logger = loggerFactory.CreateLogger<ScriptTestBase>();

            TestRequestTimeout = int.Parse(configuration["TestRequestTimeout"]);
            TestClientOptions = configuration.GetSection("HostBotClientOptions").Get<Dictionary<HostBot, DirectLineTestClientOptions>>();
        }

        public Dictionary<HostBot, DirectLineTestClientOptions> TestClientOptions { get; }

        public ILogger Logger { get; }

        public int TestRequestTimeout { get; }
    }
}
