using System.IO;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Tests
{
    [TestClass]
    public class CalendarSkillTests
    {
        public static ResourceExplorer ResourceExplorer { get; set; }

        public TestContext TestContext { get; set; }

        public static IConfiguration Configuration { get; set; }

        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            ResourceExplorer = new ResourceExplorer()
           .AddFolder(Path.Combine(TestUtils.GetProjectPath(), nameof(CalendarSkillTests)), monitorChanges: false)
           .AddFolder(Path.Combine(TestUtils.GetProjectPath(), @"..\..\"));

            Configuration = new ConfigurationBuilder()
                .AddJsonFile("testsettings.json", optional: true, reloadOnChange: false)
                .Build();
        }

        [TestMethod]
        public async Task CalendarSkillTests_Greeting()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task CalendarSkillTests_GetProfile()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task CalendarSkillTests_GetEvents()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task CalendarSkillTests_SortEvents()
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
    }
}
