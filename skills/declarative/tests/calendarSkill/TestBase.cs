using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.AI.Luis.Testing;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Tests;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.Bot.CalendarSkill.Dialogs.Tests
{
    [TestClass]
    public class TestBase
    {
        public static IConfiguration Configuration { get; set; }

        public static ResourceExplorer ResourceExplorer { get; set; }

        public TestBase(string path)
        {
            var rootFolder = Path.Combine(TestUtils.GetProjectPath(), @"..\..\calendarSkill");
            var testFolder = Path.Combine(TestUtils.GetProjectPath(), "CalendarSkillTests", path);
            Configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                    {
                        { "root", rootFolder},
                        { "luis:resources", testFolder },
                    })
                .UseLuisSettings()
                .AddJsonFile("testsettings.json", optional: true, reloadOnChange: false)
                .Build();

            ResourceExplorer = new ResourceExplorer()
               .AddFolder(rootFolder, monitorChanges: false)
               .AddFolder(testFolder)
               .RegisterType(LuisAdaptiveRecognizer.Kind, typeof(MockLuisRecognizer), new MockLuisLoader(Configuration));
        }

    }
}
