using System.IO;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Tests
{
    [TestClass]
    public class CustomActionTests
    {
        public static ResourceExplorer ResourceExplorer { get; set; }

        public TestContext TestContext { get; set; }

        public static IConfiguration Configuration { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ResourceExplorer = new ResourceExplorer()
           .AddFolder(Path.Combine(TestUtils.GetProjectPath(), "CalendarSkillTests", nameof(CustomActionTests)), monitorChanges: false)
           .AddFolder(Path.Combine(TestUtils.GetProjectPath(), @"..\..\"));

            Configuration = new ConfigurationBuilder()
                .AddJsonFile("testsettings.json", optional: true, reloadOnChange: false)
                .Build();
        }

        [TestMethod]
        public async Task CalendarSkillTests_GetProfile()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task CalendarSkillTests_AcceptEvent()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task CalendarSkillTests_TentativelyAcceptEvent()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }


        [TestMethod]
        public async Task CalendarSkillTests_DeclineEvent()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task CalendarSkillTests_DeleteEvent()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task CalendarSkillTests_GetEvents()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task CalendarSkillTests_GetWorkingHours()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task CalendarSkillTests_SettingsTest()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task CalendarSkillTests_CreateEvent()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task CalendarSkillTests_UpdateEvent()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task CalendarSkillTests_GetContacts()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task CalendarSkillTests_FindMeetingTimes()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }
    }
}
