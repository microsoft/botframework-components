﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Testing;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Teams.Tests
{
    public class TestUtils
    {
        public static IConfiguration DefaultConfiguration { get; set; } = new ConfigurationBuilder().AddInMemoryCollection().Build();

        public static string RootFolder { get; set; } = GetProjectPath();

        public static IEnumerable<object[]> GetTestScripts(string relativeFolder)
        {
            var testFolder = Path.GetFullPath(Path.Combine(RootFolder, PathUtils.NormalizePath(relativeFolder)));
            return Directory.EnumerateFiles(testFolder, "*.test.dialog", SearchOption.AllDirectories).Select(s => new object[] { Path.GetFileName(s) }).ToArray();
        }

        public static async Task RunTestScript(ResourceExplorer resourceExplorer, string resourceId = null, IConfiguration configuration = null, [CallerMemberName] string testName = null, IEnumerable<IMiddleware> middleware = null, string adapterChannel = Channels.Msteams)
        {
            var storage = new MemoryStorage();
            var convoState = new ConversationState(storage);
            var userState = new UserState(storage);

            var adapter = (TestAdapter)new TestAdapter(CreateConversation(adapterChannel, testName));

            if (middleware != null)
            {
                foreach (var m in middleware)
                {
                    adapter.Use(m);
                }
            }

            adapter.Use(new RegisterClassMiddleware<IConfiguration>(DefaultConfiguration))
                .UseStorage(storage)
                .UseBotState(userState, convoState)
                .Use(new TranscriptLoggerMiddleware(new TraceTranscriptLogger(traceActivity: false)));

            adapter.OnTurnError += async (context, err) =>
            {
                if (err.Message.EndsWith("MemoryAssertion failed"))
                {
                    throw err;
                }

                await context.SendActivityAsync(err.Message);
            };

            var script = resourceExplorer.LoadType<TestScript>(resourceId ?? $"{testName}.test.dialog");
            script.Configuration = configuration ?? new ConfigurationBuilder().AddInMemoryCollection().Build();
            script.Description ??= resourceId;
            await script.ExecuteAsync(adapter: adapter, testName: testName, resourceExplorer: resourceExplorer).ConfigureAwait(false);
        }

        public static string GetProjectPath()
        {
            var parent = Environment.CurrentDirectory;
            while (!string.IsNullOrEmpty(parent))
            {
                if (Directory.EnumerateFiles(parent, "*proj").Any())
                {
                    break;
                }

                parent = Path.GetDirectoryName(parent);
            }

            return parent;
        }

        public static ConversationReference CreateConversation(string channel, string conversationName)
        {
            return new ConversationReference
            {
                ChannelId = channel ?? Channels.Test,
                ServiceUrl = "https://test.com",
                User = new ChannelAccount("user1", "User1"),
                Bot = new ChannelAccount("bot", "Bot"),
                Conversation = new ConversationAccount(false, "personal", conversationName),
                Locale = "en-US",
            };
        }
    }
}
