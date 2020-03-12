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
        public async Task Test_TestFunctions()
        {
            var data = new Dictionary<string, object>()
            {
                { "Name", "NAME" },
                { "Address", "ADDRESS" },
            };

            CollectionAssert.Contains(ParseReplies(POISharedResponses.PointOfInterestSuggestedActionName, data), "NAME at ADDRESS");

            CollectionAssert.Contains(ParseReplies(POISharedResponses.SingleLocationFound, data), "I found the following.");
            CollectionAssert.Contains(ParseReplies(POISharedResponses.SingleLocationFound, data), "Here's a match.");
        }

        [TestMethod]
        public async Task Test_SkillModeCompletion()
        {
            await this.GetSkillTestFlow()
                .Send(GeneralTestUtterances.UnknownIntent)
                .AssertReplyOneOf(this.ParseReplies(POISharedResponses.DidntUnderstandMessage))
                .AssertReply(CheckForEoC())
                .StartTestAsync();
        }
    }
}
