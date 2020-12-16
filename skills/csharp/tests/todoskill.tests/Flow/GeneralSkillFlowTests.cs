// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ToDoSkill.Responses.Main;
using ToDoSkill.Tests.Flow.Utterances;

namespace ToDoSkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class GeneralSkillFlowTests : ToDoSkillTestBase
    {
        [TestMethod]
        public async Task Test_SkillModeCompletion()
        {
            await this.GetSkillTestFlow()
                .Send(GeneralTestUtterances.UnknownIntent)
                .AssertReplyOneOf(this.ConfusedResponse())
                .AssertReply((activity) => { Assert.AreEqual(ActivityTypes.EndOfConversation, activity.Type); })
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Help()
        {
            await this.GetTestFlow()
                .SendConversationUpdate()
                .AssertReplyOneOf(GetTemplates(ToDoMainResponses.ToDoWelcomeMessage))
                .AssertReplyOneOf(GetTemplates(ToDoMainResponses.FirstPromptMessage))
                .Send(GeneralTestUtterances.Help)
                .AssertReplyOneOf(GetTemplates(ToDoMainResponses.HelpMessage))
                .AssertReplyOneOf(GetTemplates(ToDoMainResponses.FirstPromptMessage))
                .StartTestAsync();
        }

        private string[] ConfusedResponse()
        {
            return GetTemplates(ToDoMainResponses.DidntUnderstandMessage);
        }
    }
}
