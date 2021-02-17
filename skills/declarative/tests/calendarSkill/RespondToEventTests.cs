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
    public class RespondToEventTests
    {
        public static ResourceExplorer ResourceExplorer { get; set; }

        public TestContext TestContext { get; set; }

        public static IConfiguration Configuration { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            var rootFolder = Path.Combine(TestUtils.GetProjectPath(), @"..\..\calendarSkill");
            var testFolder = Path.Combine(TestUtils.GetProjectPath(), "CalendarSkillTests", nameof(RespondToEventTests));
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
        public async Task RespondToEvent_Accept()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task RespondToEvent_Decline()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task RespondToEvent_TentativelyAccept()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task CancelEvent_asAttendee()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task CancelEvent_asOrganizer()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }
    }
}