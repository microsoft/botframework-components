// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using ITSMSkill.Models;
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
    public class TicketShowFlowTests : SkillTestBase
    {
        [TestMethod]
        public async Task ShowTest()
        {
            var navigate = new Dictionary<string, object>
            {
                { "Navigate", string.Empty }
            };

            var attribute = new Dictionary<string, object>
            {
                { "Attributes", $"Search text: {MockData.CreateTicketTitle}{Environment.NewLine}Urgency: {MockData.CreateTicketUrgencyLevel.ToString()}" }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .AssertReply(AssertContains(MainResponses.FirstPromptMessage))
                .Send(TicketShowUtterances.Show)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.TicketUpdateClose))
                .AssertReply(AssertStartsWith(TicketResponses.TicketShow, navigate))
                .Send(GeneralTestUtterances.Confirm)
                .AssertReply(AssertStartsWith(TicketResponses.ShowAttribute))
                .Send(NonLuisUtterances.Text)
                .AssertReply(AssertContains(SharedResponses.InputSearch))
                .Send(MockData.CreateTicketTitle)
                .AssertReply(AssertStartsWith(TicketResponses.ShowAttribute))
                .Send(NonLuisUtterances.Urgency)
                .AssertReply(AssertStartsWith(SharedResponses.InputUrgency))
                .Send(NonLuisUtterances.CreateTicketUrgency)
                .AssertReply(AssertStartsWith(TicketResponses.ShowAttribute))
                .Send(NonLuisUtterances.No)
                .AssertReply(AssertStartsWith(TicketResponses.ShowConstraints, attribute))
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.TicketUpdateClose))
                .AssertReply(AssertStartsWith(TicketResponses.TicketShow, navigate))
                .Send(GeneralTestUtterances.Reject)
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task ShowThenCloseTest()
        {
            var navigate = new Dictionary<string, object>
            {
                { "Navigate", string.Empty }
            };

            var attribute = new Dictionary<string, object>
            {
                { "Attributes", string.Empty }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .AssertReply(AssertContains(MainResponses.FirstPromptMessage))
                .Send(TicketShowUtterances.Show)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.TicketUpdateClose))
                .AssertReply(AssertStartsWith(TicketResponses.TicketShow, navigate))
                .Send(TicketCloseUtterances.Close)
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
        public async Task ShowWithTitleTest()
        {
            var navigate = new Dictionary<string, object>
            {
                { "Navigate", string.Empty }
            };

            var attribute = new Dictionary<string, object>
            {
                { "Attributes", $"Search text: {MockData.CreateTicketTitle}" }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .AssertReply(AssertContains(MainResponses.FirstPromptMessage))
                .Send(TicketShowUtterances.ShowWithTitle)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertStartsWith(TicketResponses.ShowConstraints, attribute))
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.TicketUpdateClose))
                .AssertReply(AssertStartsWith(TicketResponses.TicketShow, navigate))
                .Send(GeneralTestUtterances.Reject)
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task ShowWithStateTest()
        {
            var navigate = new Dictionary<string, object>
            {
                { "Navigate", string.Empty }
            };

            var attribute = new Dictionary<string, object>
            {
                { "Attributes", $"State: {TicketState.Active.ToLocalizedString()}" }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .AssertReply(AssertContains(MainResponses.FirstPromptMessage))
                .Send(TicketShowUtterances.ShowWithState)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertStartsWith(TicketResponses.ShowConstraints, attribute))
                .AssertReply(AssertContains(SharedResponses.ResultsIndicator, null, CardStrings.TicketUpdateClose, CardStrings.TicketUpdateClose))
                .AssertReply(AssertStartsWith(TicketResponses.TicketShow, navigate))
                .Send(GeneralTestUtterances.Reject)
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task ShowActionTest()
        {
            var navigate = new Dictionary<string, object>
            {
                { "Navigate", string.Empty }
            };

            var attribute = new Dictionary<string, object>
            {
                { "Attributes", $"Search text: {MockData.CreateTicketTitle}{Environment.NewLine}Urgency: {MockData.CreateTicketUrgencyLevel.ToString()}" }
            };

            await this.GetSkillTestFlow()
                .Send(TicketShowUtterances.ShowAction)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.TicketUpdateClose))
                .AssertReply(AssertStartsWith(TicketResponses.TicketShow, navigate))
                .Send(GeneralTestUtterances.Confirm)
                .AssertReply(AssertStartsWith(TicketResponses.ShowAttribute))
                .Send(NonLuisUtterances.Text)
                .AssertReply(AssertContains(SharedResponses.InputSearch))
                .Send(MockData.CreateTicketTitle)
                .AssertReply(AssertStartsWith(TicketResponses.ShowAttribute))
                .Send(NonLuisUtterances.Urgency)
                .AssertReply(AssertStartsWith(SharedResponses.InputUrgency))
                .Send(NonLuisUtterances.CreateTicketUrgency)
                .AssertReply(AssertStartsWith(TicketResponses.ShowAttribute))
                .Send(NonLuisUtterances.No)
                .AssertReply(AssertStartsWith(TicketResponses.ShowConstraints, attribute))
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.TicketUpdateClose))
                .AssertReply(AssertStartsWith(TicketResponses.TicketShow, navigate))
                .Send(GeneralTestUtterances.Reject)
                .AssertReply(SkillActionEndMessage(true))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task ShowThenCloseActionTest()
        {
            var navigate = new Dictionary<string, object>
            {
                { "Navigate", string.Empty }
            };

            var attribute = new Dictionary<string, object>
            {
                { "Attributes", string.Empty }
            };

            await this.GetSkillTestFlow()
                .Send(TicketShowUtterances.ShowAction)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.TicketUpdateClose))
                .AssertReply(AssertStartsWith(TicketResponses.TicketShow, navigate))
                .Send(TicketCloseUtterances.Close)
                .AssertReply(AssertStartsWith(TicketResponses.TicketShow, navigate))
                .Send(GeneralTestUtterances.Reject)
                .AssertReply(SkillActionEndMessage(true))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task ShowWithTitleActionTest()
        {
            var navigate = new Dictionary<string, object>
            {
                { "Navigate", string.Empty }
            };

            var attribute = new Dictionary<string, object>
            {
                { "Attributes", $"Search text: {MockData.CreateTicketTitle}" }
            };

            await this.GetSkillTestFlow()
                .Send(TicketShowUtterances.ShowWithTitleAction)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertStartsWith(TicketResponses.ShowConstraints, attribute))
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.TicketUpdateClose))
                .AssertReply(AssertStartsWith(TicketResponses.TicketShow, navigate))
                .Send(GeneralTestUtterances.Reject)
                .AssertReply(SkillActionEndMessage(true))
                .StartTestAsync();
        }
    }
}
