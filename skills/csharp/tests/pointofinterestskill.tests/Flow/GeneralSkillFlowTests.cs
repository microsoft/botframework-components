// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PointOfInterestSkill.Responses.Main;
using PointOfInterestSkill.Responses.Shared;
using PointOfInterestSkill.Tests.Flow.Utterances;

namespace PointOfInterestSkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class GeneralSkillFlowTests : PointOfInterestSkillTestBase
    {
        /// <summary>
        /// Test test functions that use external functions.
        /// </summary>
        /// <returns>Task object.</returns>
        [TestMethod]
        public async Task TestFunctionsTest()
        {
            var data = new Dictionary<string, object>()
            {
                { "Name", "NAME" },
                { "Address", "ADDRESS" },
            };

            CollectionAssert.Contains(ParseReplies(POISharedResponses.PointOfInterestSuggestedActionName, data), "NAME at ADDRESS");

            CollectionAssert.Contains(ParseReplies(POISharedResponses.SingleRouteFound, data), "I found a route. Let's begin!");
            CollectionAssert.Contains(ParseReplies(POISharedResponses.SingleRouteFound, data), "I found a route. Let's start!");
        }

        [TestMethod]
        public async Task HelpTest()
        {
            await this.GetTestFlow()
                .SendConversationUpdate()
                .AssertReply(AssertContains(POIMainResponses.PointOfInterestWelcomeMessage, null))
                .Send(GeneralTestUtterances.Help)
                .AssertReply(AssertContains(POIMainResponses.HelpMessage, null))
                .AssertReply(AssertContains(POIMainResponses.PointOfInterestWelcomeMessage, null))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task CancelTest()
        {
            await this.GetTestFlow()
                .SendConversationUpdate()
                .AssertReply(AssertContains(POIMainResponses.PointOfInterestWelcomeMessage, null))
                .Send(GeneralTestUtterances.Cancel)
                .AssertReply(AssertContains(POISharedResponses.CancellingMessage, null))
                .AssertReply(AssertContains(POIMainResponses.FirstPromptMessage, null))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task SkillModeCompletionTest()
        {
            await this.GetSkillTestFlow()
                .Send(GeneralTestUtterances.UnknownIntent)
                .AssertReplyOneOf(this.ParseReplies(POISharedResponses.DidntUnderstandMessage))
                .AssertReply(CheckForEoC())
                .StartTestAsync();
        }
    }
}
