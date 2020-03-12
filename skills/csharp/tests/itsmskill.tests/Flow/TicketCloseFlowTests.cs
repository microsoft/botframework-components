﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Specialized;
using System.Threading.Tasks;
using ITSMSkill.Responses.Knowledge;
using ITSMSkill.Responses.Main;
using ITSMSkill.Responses.Shared;
using ITSMSkill.Responses.Ticket;
using ITSMSkill.Tests.API.Fakes;
using ITSMSkill.Tests.Flow.Strings;
using ITSMSkill.Tests.Flow.Utterances;
using ITSMSkill.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ITSMSkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class TicketCloseFlowTests : SkillTestBase
    {
        [TestMethod]
        public async Task CloseTest()
        {
            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(TicketCloseUtterances.Close)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(SharedResponses.InputTicketNumber))
                .Send(MockData.CloseTicketNumber)
                .AssertReply(AssertContains(TicketResponses.TicketTarget, null, CardStrings.Ticket))
                .AssertReply(AssertContains(SharedResponses.InputReason))
                .Send(MockData.CloseTicketReason)
                .AssertReply(AssertContains(TicketResponses.TicketClosed, null, CardStrings.TicketUpdate))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task CloseWithNumberReasonTest()
        {
            var confirmReason = new Dictionary<string, string>
            {
                { "Reason", MockData.CloseTicketReason }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(TicketCloseUtterances.CloseWithNumberReason)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(TicketResponses.TicketTarget, null, CardStrings.Ticket))
                .AssertReply(AssertStartsWith(SharedResponses.ConfirmReason, confirmReason))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(TicketResponses.TicketClosed, null, CardStrings.TicketUpdate))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task CloseTestAction()
        {
            await this.GetSkillTestFlow()
                .Send(TicketCloseUtterances.CloseAction)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(SharedResponses.InputTicketNumber))
                .Send(MockData.CloseTicketNumber)
                .AssertReply(AssertContains(TicketResponses.TicketTarget, null, CardStrings.Ticket))
                .AssertReply(AssertContains(SharedResponses.InputReason))
                .Send(MockData.CloseTicketReason)
                .AssertReply(AssertContains(TicketResponses.TicketClosed, null, CardStrings.TicketUpdate))
                .AssertReply(SkillActionEndMessage(true))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task CloseWithNumberReasonActionTest()
        {
            var confirmReason = new Dictionary<string, string>
            {
                { "Reason", MockData.CloseTicketReason }
            };

            await this.GetSkillTestFlow()
                .Send(TicketCloseUtterances.CloseWithNumberReasonAction)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(TicketResponses.TicketTarget, null, CardStrings.Ticket))
                .AssertReply(AssertStartsWith(SharedResponses.ConfirmReason, confirmReason))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(TicketResponses.TicketClosed, null, CardStrings.TicketUpdate))
                .AssertReply(SkillActionEndMessage(true))
                .StartTestAsync();
        }
    }
}
