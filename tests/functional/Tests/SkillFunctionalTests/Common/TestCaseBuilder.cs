// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using TranscriptTestRunner;

namespace SkillFunctionalTests.Common
{
    public class TestCaseBuilder
    {
        public IEnumerable<object[]> BuildTestCases(List<string> channelIds, List<string> deliveryModes, List<HostBot> hostBots, List<string> targetSkills, List<string> scripts, Func<TestCase, bool> shouldExclude = null)
        {
            var testCases = new List<object[]>();
            var count = 1;
            foreach (var channelId in channelIds)
            {
                foreach (var script in scripts)
                {
                    foreach (var deliveryMode in deliveryModes)
                    {
                        foreach (var hostBot in hostBots)
                        {
                            foreach (var targetSkill in targetSkills)
                            {
                                var testCase = new TestCase
                                {
                                    Description = $"{script}, {hostBot}, {targetSkill}, {channelId}, {deliveryMode}",
                                    ChannelId = channelId,
                                    DeliveryMode = deliveryMode,
                                    HostBot = hostBot,
                                    TargetSkill = targetSkill,
                                    Script = script
                                };

                                if (!ExcludeTestCase(shouldExclude, testCase))
                                {
                                    testCases.Add(new object[] { new TestCaseDataObject(testCase) });
                                    count++;
                                }
                            }
                        }
                    }
                }
            }

            return testCases;
        }

        private bool ExcludeTestCase(Func<TestCase, bool> shouldExclude, TestCase testCase)
        {
            if (shouldExclude == null)
            {
                return false;
            }

            return shouldExclude(testCase);
        }
    }
}
