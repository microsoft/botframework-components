// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SkillFunctionalTests.Common;
using TranscriptTestRunner;
using TranscriptTestRunner.XUnit;
using Xunit;
using Xunit.Abstractions;

namespace SkillFunctionalTests.CardActions
{
    [Trait("TestCategory", "CardActions")]
    public class CardActionsTests : ScriptTestBase
    {
        private readonly string _testScriptsFolder = Directory.GetCurrentDirectory() + @"/CardActions/TestScripts";

        public CardActionsTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public static IEnumerable<object[]> TestCases()
        {
            var channelIds = new List<string> { Channels.Directline };
            
            var deliverModes = new List<string>
            {
                DeliveryModes.Normal,
                DeliveryModes.ExpectReplies
            };

            var hostBots = new List<HostBot>
            {
                HostBot.WaterfallHostBotDotNet,
                HostBot.WaterfallHostBotJS,
                HostBot.WaterfallHostBotPython,

                // TODO: Enable this when the port to composer is ready
                //HostBot.ComposerHostBotDotNet
            };

            var targetSkills = new List<string>
            {
                SkillBotNames.WaterfallSkillBotDotNet,
                SkillBotNames.WaterfallSkillBotJS,
                SkillBotNames.WaterfallSkillBotPython,

                // TODO: Enable this when the port to composer is ready
                //SkillBotNames.ComposerSkillBotDotNet
            };

            var scripts = new List<string>
            {
                "BotAction.json",
                "TaskModule.json",
                "SubmitAction.json",
                "Hero.json",
                "Thumbnail.json",
                "Receipt.json",
                "SignIn.json",
                "Carousel.json",
                "List.json",
                "O365.json",
                "Animation.json",
                "Audio.json",
                "Video.json"
            };

            var testCaseBuilder = new TestCaseBuilder();

            // This local function is used to exclude ExpectReplies, O365 and WaterfallSkillBotPython test cases
            static bool ShouldExclude(TestCase testCase)
            {
                if (testCase.Script == "O365.json")
                {
                    // BUG: O365 fails with ExpectReplies for WaterfallSkillBotPython (remove when https://github.com/microsoft/BotFramework-FunctionalTests/issues/328 is fixed).
                    if (testCase.TargetSkill == SkillBotNames.WaterfallSkillBotPython && testCase.DeliveryMode == DeliveryModes.ExpectReplies)
                    {
                        return true;
                    }
                }

                return false;
            }

            var testCases = testCaseBuilder.BuildTestCases(channelIds, deliverModes, hostBots, targetSkills, scripts, ShouldExclude);
            foreach (var testCase in testCases)
            {
                yield return testCase;
            }
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public async Task RunTestCases(TestCaseDataObject testData)
        {
            var testCase = testData.GetObject<TestCase>();
            Logger.LogInformation(JsonConvert.SerializeObject(testCase, Formatting.Indented));

            var options = TestClientOptions[testCase.HostBot];
            var runner = new XUnitTestRunner(new TestClientFactory(testCase.ChannelId, options, Logger).GetTestClient(), TestRequestTimeout, Logger);

            var testParams = new Dictionary<string, string>
            {
                { "DeliveryMode", testCase.DeliveryMode },
                { "TargetSkill", testCase.TargetSkill }
            };

            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, "WaterfallGreeting.json"), testParams);
            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, testCase.Script), testParams);
        }
    }
}
