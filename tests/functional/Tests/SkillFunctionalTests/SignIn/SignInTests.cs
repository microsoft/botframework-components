// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace SkillFunctionalTests.SignIn
{
    [Trait("TestCategory", "SignIn")]
    public class SignInTests : ScriptTestBase
    {
        private readonly string _testScriptsFolder = Directory.GetCurrentDirectory() + @"/SignIn/TestScripts";

        public SignInTests(ITestOutputHelper output)
            : base(output)
        {
        }

        public static IEnumerable<object[]> TestCases()
        {
            var channelIds = new List<string> { Channels.Directline };
            var deliverModes = new List<string>
            {
                DeliveryModes.Normal,
                DeliveryModes.ExpectReplies,
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
                "SignIn1.json"
            };

            var testCaseBuilder = new TestCaseBuilder();

            var testCases = testCaseBuilder.BuildTestCases(channelIds, deliverModes, hostBots, targetSkills, scripts);
            foreach (var testCase in testCases)
            {
                yield return testCase;
            }
        }

        [Theory]
        [MemberData(nameof(TestCases))]
        public async Task RunTestCases(TestCaseDataObject testData)
        {
            var signInUrl = string.Empty;
            var testCase = testData.GetObject<TestCase>();
            Logger.LogInformation(JsonConvert.SerializeObject(testCase, Formatting.Indented));

            var options = TestClientOptions[testCase.HostBot];
            var runner = new XUnitTestRunner(new TestClientFactory(testCase.ChannelId, options, Logger).GetTestClient(), TestRequestTimeout, Logger);

            var testParams = new Dictionary<string, string>
            {
                { "DeliveryMode", testCase.DeliveryMode },
                { "TargetSkill", testCase.TargetSkill }
            };

            // Execute the first part of the conversation.
            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, testCase.Script), testParams);

            await runner.AssertReplyAsync(activity =>
            {
                Assert.Equal(ActivityTypes.Message, activity.Type);
                Assert.True(activity.Attachments.Count > 0);

                var card = JsonConvert.DeserializeObject<SigninCard>(JsonConvert.SerializeObject(activity.Attachments.FirstOrDefault().Content));
                signInUrl = card.Buttons[0].Value?.ToString();

                Assert.False(string.IsNullOrEmpty(signInUrl));
            });

            // Execute the SignIn.
            await runner.ClientSignInAsync(signInUrl);

            // Execute the rest of the conversation passing the messageId.
            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, "SignIn2.json"), testParams);
        }
    }
}
