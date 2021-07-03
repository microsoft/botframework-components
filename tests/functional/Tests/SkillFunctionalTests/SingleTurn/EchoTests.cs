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

namespace SkillFunctionalTests.SingleTurn
{
    [Trait("TestCategory", "SingleTurn")]
    public class EchoTests : ScriptTestBase
    {
        private readonly string _testScriptsFolder = Directory.GetCurrentDirectory() + @"/SingleTurn/TestScripts";

        public EchoTests(ITestOutputHelper output)
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
                HostBot.EmptyBotDotNetWebApp,
                HostBot.EmptyBotDotNetFunctions,
                HostBot.EmptyBotJSWebApp,
                HostBot.EmptyBotJSFunctions
            };

            var targetSkills = new List<string>
            {
            };

            var scripts = new List<string> { "EchoMultiSkill.json" };

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
            var testCase = testData.GetObject<TestCase>();
            Logger.LogInformation(JsonConvert.SerializeObject(testCase, Formatting.Indented));

            var options = TestClientOptions[testCase.HostBot];

            var runner = new XUnitTestRunner(new TestClientFactory(testCase.ChannelId, options, Logger).GetTestClient(), TestRequestTimeout, Logger);

            var testParams = new Dictionary<string, string>
            {
                { "DeliveryMode", testCase.DeliveryMode },
                { "TargetSkill", testCase.TargetSkill }
            };

            await runner.RunTestAsync(Path.Combine(_testScriptsFolder, testCase.Script), testParams);
        }
    }
}
