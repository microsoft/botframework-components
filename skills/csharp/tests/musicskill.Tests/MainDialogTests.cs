// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MusicSkill.Responses.Main;
using MusicSkill.Responses.Shared;
using MusicSkill.Tests.Utterances;

namespace MusicSkill.Tests
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class MainDialogTests : SkillTestBase
    {
        [TestMethod]
        public async Task Test_Intro_Message()
        {
            await GetTestFlow()
                .Send(new Activity()
                {
                    Type = ActivityTypes.ConversationUpdate,
                    MembersAdded = new List<ChannelAccount>() { new ChannelAccount("user") }
                })
                .AssertReplyOneOf(ParseReplies(MainResponses.WelcomeMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Help_Intent()
        {
            await GetTestFlow()
                .Send(GeneralUtterances.Help)
                .AssertReplyOneOf(ParseReplies(MainResponses.HelpMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Unhandled_Message()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(MainResponses.FirstPromptMessage))
                .Send(PlayMusicDialogUtterances.None)
                .AssertReplyOneOf(ParseReplies(SharedResponses.DidntUnderstandMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Help_Interruption()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(MainResponses.FirstPromptMessage))
                .Send(GeneralUtterances.Help)
                .AssertReplyOneOf(ParseReplies(MainResponses.HelpMessage))
                .AssertReplyOneOf(ParseReplies(MainResponses.FirstPromptMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_Cancel_Interruption()
        {
            await GetTestFlow()
                .Send(string.Empty)
                .AssertReplyOneOf(ParseReplies(MainResponses.FirstPromptMessage))
                .Send(GeneralUtterances.Cancel)
                .AssertReplyOneOf(ParseReplies(MainResponses.CancelMessage))
                .AssertReplyOneOf(ParseReplies(MainResponses.FirstPromptMessage))
                .StartTestAsync();
        }
    }
}