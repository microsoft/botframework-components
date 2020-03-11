// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PhoneSkill.Responses.Main;
using PhoneSkill.Responses.Shared;
using PhoneSkill.Tests.Flow.Utterances;

namespace PhoneSkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class MainDialogTests : PhoneSkillTestBase
    {
        [TestMethod]
        public async Task Test_Help_Intent()
        {
            await GetTestFlow()
                .SendConversationUpdate()
                .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
                .Send(GeneralUtterances.Help)
                .AssertReply(Message(PhoneMainResponses.HelpMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Unhandled_Message()
        {
            await GetTestFlow()
                .SendConversationUpdate()
                .AssertReply(Message(PhoneMainResponses.WelcomeMessage))
                .Send(GeneralUtterances.Incomprehensible)
                .AssertReply(Message(PhoneSharedResponses.DidntUnderstandMessage))
                .StartTestAsync();
        }
    }
}
