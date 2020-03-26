﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using ITSMSkill.Responses.Main;
using ITSMSkill.Tests.Flow.Utterances;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ITSMSkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class GeneralFlowTests : SkillTestBase
    {
        [TestMethod]
        public async Task HelpTest()
        {
            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .AssertReply(AssertContains(MainResponses.FirstPromptMessage))
                .Send(GeneralTestUtterances.Help)
                .AssertReply(AssertContains(MainResponses.HelpMessage))
                .AssertReply(AssertContains(MainResponses.FirstPromptMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task CancelTest()
        {
            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .AssertReply(AssertContains(MainResponses.FirstPromptMessage))
                .Send(GeneralTestUtterances.Cancel)
                .AssertReply(AssertContains(MainResponses.CancelMessage))
                .AssertReply(AssertContains(MainResponses.FirstPromptMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task SkillModeCompletionTest()
        {
            await this.GetSkillTestFlow()
                .Send(GeneralTestUtterances.UnknownIntent)
                .AssertReply(AssertContains(MainResponses.FeatureNotAvailable))
                .AssertReply(SkillActionEndMessage())
                .StartTestAsync();
        }
    }
}
