using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Bot.CalendarSkill.Dialogs.Tests;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Tests
{
    [TestClass]
    public class AvailabilityTests : TestBase
    {
        public AvailabilityTests() : base(nameof(AvailabilityTests))
        {
        }

        [TestMethod]
        public async Task GetAvailabilityBreaks()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task GetAvailabilityFirst()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }

        [TestMethod]
        public async Task GetAvailabilityLast()
        {
            await TestUtils.RunTestScript(ResourceExplorer, configuration: Configuration);
        }
    }
}