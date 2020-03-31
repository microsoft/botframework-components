// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
    public class TicketUpdateFlowTests : SkillTestBase
    {
        [TestMethod]
        public async Task UpdateTest()
        {
            var attribute = new Dictionary<string, object>()
            {
                { "Attributes", $"Title: {MockData.CreateTicketTitle}" }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .AssertReply(AssertContains(MainResponses.FirstPromptMessage))
                .Send(TicketUpdateUtterances.Update)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(SharedResponses.InputTicketNumber))
                .Send(MockData.CreateTicketNumber)
                .AssertReply(AssertContains(TicketResponses.TicketTarget, null, CardStrings.Ticket))
                .AssertReply(AssertContains(TicketResponses.UpdateAttribute))
                .Send(NonLuisUtterances.Title)
                .AssertReply(AssertContains(SharedResponses.InputTitle))
                .Send(MockData.CreateTicketTitle)
                .AssertReply(AssertStartsWith(TicketResponses.ShowUpdates, attribute))
                .AssertReply(AssertContains(TicketResponses.UpdateAttribute))
                .Send(NonLuisUtterances.No)
                .AssertReply(AssertContains(TicketResponses.TicketUpdated, null, CardStrings.TicketUpdateClose))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task UpdateWithNumberUrgencyTest()
        {
            var attribute = new Dictionary<string, object>()
            {
                { "Attributes", $"Urgency: {MockData.CreateTicketUrgencyLevel.ToString()}" }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .AssertReply(AssertContains(MainResponses.FirstPromptMessage))
                .Send(TicketUpdateUtterances.UpdateWithNumberUrgency)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(TicketResponses.TicketTarget, null, CardStrings.Ticket))
                .AssertReply(AssertStartsWith(TicketResponses.ShowUpdates, attribute))
                .AssertReply(AssertContains(TicketResponses.UpdateAttribute))
                .Send(NonLuisUtterances.No)
                .AssertReply(AssertContains(TicketResponses.TicketUpdated, null, CardStrings.TicketUpdateClose))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task UpdateActionTest()
        {
            var attribute = new Dictionary<string, object>()
            {
                { "Attributes", $"Title: {MockData.CreateTicketTitle}" }
            };

            await this.GetSkillTestFlow()
                .Send(TicketUpdateUtterances.UpdateAction)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(SharedResponses.InputTicketNumber))
                .Send(MockData.CreateTicketNumber)
                .AssertReply(AssertContains(TicketResponses.TicketTarget, null, CardStrings.Ticket))
                .AssertReply(AssertContains(TicketResponses.UpdateAttribute))
                .Send(NonLuisUtterances.Title)
                .AssertReply(AssertContains(SharedResponses.InputTitle))
                .Send(MockData.CreateTicketTitle)
                .AssertReply(AssertStartsWith(TicketResponses.ShowUpdates, attribute))
                .AssertReply(AssertContains(TicketResponses.UpdateAttribute))
                .Send(NonLuisUtterances.No)
                .AssertReply(AssertContains(TicketResponses.TicketUpdated, null, CardStrings.TicketUpdateClose))
                .AssertReply(SkillActionEndMessage(true))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task UpdateWithNumberUrgencyDescriptionActionTest()
        {
            var attribute = new Dictionary<string, object>()
            {
                { "Attributes", $"Description: {MockData.CreateTicketDescription}{Environment.NewLine}Urgency: {MockData.CreateTicketUrgencyLevel.ToString()}" }
            };

            await this.GetSkillTestFlow()
                .Send(TicketUpdateUtterances.UpdateWithNumberUrgencyDescriptionAction)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(TicketResponses.TicketTarget, null, CardStrings.Ticket))
                .AssertReply(AssertStartsWith(TicketResponses.ShowUpdates, attribute))
                .AssertReply(AssertContains(TicketResponses.UpdateAttribute))
                .Send(NonLuisUtterances.No)
                .AssertReply(AssertContains(TicketResponses.TicketUpdated, null, CardStrings.TicketUpdateClose))
                .AssertReply(SkillActionEndMessage(true))
                .StartTestAsync();
        }
    }
}
