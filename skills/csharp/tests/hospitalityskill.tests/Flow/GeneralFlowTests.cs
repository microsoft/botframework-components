// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using HospitalitySkill.Responses.Main;
using HospitalitySkill.Responses.Shared;
using HospitalitySkill.Tests.Flow.Utterances;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HospitalitySkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class GeneralFlowTests : HospitalitySkillTestBase
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
                .Send(GeneralTestUtterances.None)
                .AssertReply(AssertContains(SharedResponses.DidntUnderstandMessage))
                .AssertReply(SkillActionEndMessage(null))
                .StartTestAsync();
        }
}
}
