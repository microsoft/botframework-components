﻿using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.Luis.Testing;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Tests
{
    [TestClass]
    public class DialogTests
    {
        public static ResourceExplorer ResourceExplorer { get; set; }

        public TestContext TestContext { get; set; }

        public static IConfiguration Configuration { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var rootFolder = Path.Combine(TestUtils.GetProjectPath(), @"..\..\");
            var testFolder = Path.Combine(TestUtils.GetProjectPath(), "CalendarSkillTests", nameof(DialogTests));
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("testsettings.json", optional: true, reloadOnChange: false)
                .AddInMemoryCollection(new Dictionary<string, string>
                    {
                        { "root", rootFolder},
                        { "luis:resources", testFolder },
                    })
                .UseLuisSettings()
                .Build();

            ResourceExplorer = new ResourceExplorer()
           .AddFolder(rootFolder, monitorChanges: false)
           .AddFolder(testFolder)
           .RegisterType(LuisAdaptiveRecognizer.Kind, typeof(MockLuisRecognizer), new MockLuisLoader(Configuration));
        }

        [TestMethod]
        public async Task Greeting()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task GetEventTitleByAttendees()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task GetEventTitleWithoutCondition()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task GetEventTitleReprompt()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task GetEventTitleNoResult()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task GetEventLocationByAttendees()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task GetEventLocationOnlineMeeting()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task AcceptEvent()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task TentativelyAcceptEvent()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }


        [TestMethod]
        public async Task DeclineEvent()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task CancelEvent()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task CancelEventNotOrganizer()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task CreateEvent()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task UpdateEvent()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }
    }
}
