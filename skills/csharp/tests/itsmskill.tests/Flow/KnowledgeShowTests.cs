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
    public class KnowledgeShowTests : SkillTestBase
    {
        [TestMethod]
        public async Task ShowTest()
        {
            var navigate = new StringDictionary
            {
                { "Navigate", string.Empty }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(KnowledgeShowUtterances.Show)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(SharedResponses.InputSearch))
                .Send(MockData.CreateTicketTitle)
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.Knowledge))
                .AssertReply(AssertStartsWith(KnowledgeResponses.IfFindWanted, navigate))
                .Send(GeneralTestUtterances.Confirm)
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task ShowAndHelpTest()
        {
            var navigate = new StringDictionary
            {
                { "Navigate", string.Empty }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(KnowledgeShowUtterances.Show)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(SharedResponses.InputSearch))
                .Send(MockData.CreateTicketTitle)
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.Knowledge))
                .AssertReply(AssertStartsWith(KnowledgeResponses.IfFindWanted, navigate))
                .Send(GeneralTestUtterances.Help)
                .AssertReply(AssertContains(MainResponses.HelpMessage))
                .AssertReply(AssertStartsWith(KnowledgeResponses.IfFindWanted, navigate))
                .Send(GeneralTestUtterances.Confirm)
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task ShowAndCancelTest()
        {
            var navigate = new StringDictionary
            {
                { "Navigate", string.Empty }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(KnowledgeShowUtterances.Show)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(SharedResponses.InputSearch))
                .Send(MockData.CreateTicketTitle)
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.Knowledge))
                .AssertReply(AssertStartsWith(KnowledgeResponses.IfFindWanted, navigate))
                .Send(GeneralTestUtterances.Cancel)
                .AssertReply(AssertContains(MainResponses.CancelMessage))
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task ShowThenCreateTest()
        {
            var navigate = new StringDictionary
            {
                { "Navigate", string.Empty }
            };

            var confirmTitle = new StringDictionary
            {
                { "Title", MockData.CreateTicketTitle }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(KnowledgeShowUtterances.Show)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(SharedResponses.InputSearch))
                .Send(MockData.CreateTicketTitle)
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.Knowledge))
                .AssertReply(AssertStartsWith(KnowledgeResponses.IfFindWanted, navigate))
                .Send(GeneralTestUtterances.Reject)
                .AssertReply(AssertStartsWith(KnowledgeResponses.IfCreateTicket))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertStartsWith(SharedResponses.ConfirmTitle, confirmTitle))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(SharedResponses.InputDescription))
                .Send(MockData.CreateTicketDescription)
                .AssertReply(AssertStartsWith(SharedResponses.InputUrgency))
                .Send(NonLuisUtterances.CreateTicketUrgency)
                .AssertReply(AssertContains(TicketResponses.TicketCreated, null, CardStrings.TicketUpdateClose))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }
    }
}
