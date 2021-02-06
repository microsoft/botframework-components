using System.IO;
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
    public class UpdateEventTests
    {
        public static ResourceExplorer ResourceExplorer { get; set; }

        public TestContext TestContext { get; set; }

        public static IConfiguration Configuration { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var rootFolder = Path.Combine(TestUtils.GetProjectPath(), @"..\..\");
            var testFolder = Path.Combine(TestUtils.GetProjectPath(), "CalendarSkillTests", nameof(UpdateEventTests));
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
        public async Task UpdateEvent_basic_attendees()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task UpdateEvent_basic_datetime()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task UpdateEvent_basic_description()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task UpdateEvent_basic_duration()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task UpdateEvent_basic_location()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task UpdateEvent_basic_onlineMeeting()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task UpdateEvent_basic_title()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task UpdateEvent_interruption_setAttendeesAdd()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task UpdateEvent_interruption_setAttendeesRemove()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task UpdateEvent_interruption_setDateTime()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task UpdateEvent_interruption_setDescription()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task UpdateEvent_interruption_setDuration()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task UpdateEvent_interruption_setLocation()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task UpdateEvent_interruption_setOnlineMeeting()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task UpdateEvent_interruption_setTitle()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task UpdateEvent_trigger_setAttendeesAdd()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task UpdateEvent_trigger_setAttendeesRemove()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task UpdateEvent_trigger_setDateTime()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task UpdateEvent_trigger_setDescription()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task UpdateEvent_trigger_setDuration()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task UpdateEvent_trigger_setLocation()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task UpdateEvent_trigger_setOnlineMeeting()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task UpdateEvent_trigger_setTitle()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }
    }
}