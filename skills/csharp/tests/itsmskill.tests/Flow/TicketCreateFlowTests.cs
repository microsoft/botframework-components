// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
    public class TicketCreateFlowTests : SkillTestBase
    {
        [TestMethod]
        public async Task CreateTest()
        {
            var navigate = new Dictionary<string, string>
            {
                { "Navigate", string.Empty }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(TicketCreateUtterances.Create)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(SharedResponses.InputTitle))
                .Send(MockData.CreateTicketTitle)
                .AssertReply(AssertContains(KnowledgeResponses.ShowExistingToSolve))
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.Knowledge))
                .AssertReply(AssertStartsWith(KnowledgeResponses.IfExistingSolve, navigate))
                .Send(GeneralTestUtterances.Reject)
                .AssertReply(AssertContains(SharedResponses.InputDescription))
                .Send(MockData.CreateTicketDescription)
                .AssertReply(AssertStartsWith(SharedResponses.InputUrgency))
                .Send(NonLuisUtterances.CreateTicketUrgency)
                .AssertReply(AssertContains(TicketResponses.TicketCreated, null, CardStrings.TicketUpdateClose))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task CreateExistingSolveTest()
        {
            var navigate = new Dictionary<string, string>
            {
                { "Navigate", string.Empty }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(TicketCreateUtterances.Create)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(SharedResponses.InputTitle))
                .Send(MockData.CreateTicketTitle)
                .AssertReply(AssertContains(KnowledgeResponses.ShowExistingToSolve))
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.Knowledge))
                .AssertReply(AssertStartsWith(KnowledgeResponses.IfExistingSolve, navigate))
                .Send(GeneralTestUtterances.Confirm)
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task CreateWithTitleUrgencyTest()
        {
            var confirmTitle = new Dictionary<string, string>
            {
                { "Title", MockData.CreateTicketTitle }
            };

            var navigate = new Dictionary<string, string>
            {
                { "Navigate", string.Empty }
            };

            var confirmUrgency = new Dictionary<string, string>
            {
                { "Urgency", MockData.CreateTicketUrgencyLevel.ToLocalizedString() }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(TicketCreateUtterances.CreateWithTitleUrgency)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertStartsWith(SharedResponses.ConfirmTitle, confirmTitle))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(KnowledgeResponses.ShowExistingToSolve))
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.Knowledge))
                .AssertReply(AssertStartsWith(KnowledgeResponses.IfExistingSolve, navigate))
                .Send(GeneralTestUtterances.Reject)
                .AssertReply(AssertContains(SharedResponses.InputDescription))
                .Send(MockData.CreateTicketDescription)
                .AssertReply(AssertStartsWith(SharedResponses.ConfirmUrgency, confirmUrgency))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(TicketResponses.TicketCreated, null, CardStrings.TicketUpdateClose))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task CreateWithTitleUrgencyNotConfirmTest()
        {
            var confirmTitle = new Dictionary<string, string>
            {
                { "Title", MockData.CreateTicketTitle }
            };

            var navigate = new Dictionary<string, string>
            {
                { "Navigate", string.Empty }
            };

            var confirmUrgency = new Dictionary<string, string>
            {
                { "Urgency", MockData.CreateTicketUrgencyLevel.ToLocalizedString() }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(TicketCreateUtterances.CreateWithTitleUrgency)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertStartsWith(SharedResponses.ConfirmTitle, confirmTitle))
                .Send(NonLuisUtterances.No)
                .AssertReply(AssertContains(SharedResponses.InputTitle))
                .Send(MockData.CreateTicketTitle)
                .AssertReply(AssertContains(KnowledgeResponses.ShowExistingToSolve))
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.Knowledge))
                .AssertReply(AssertStartsWith(KnowledgeResponses.IfExistingSolve, navigate))
                .Send(GeneralTestUtterances.Reject)
                .AssertReply(AssertContains(SharedResponses.InputDescription))
                .Send(MockData.CreateTicketDescription)
                .AssertReply(AssertStartsWith(SharedResponses.ConfirmUrgency, confirmUrgency))
                .Send(NonLuisUtterances.No)
                .AssertReply(AssertStartsWith(SharedResponses.InputUrgency))
                .Send(NonLuisUtterances.CreateTicketUrgency)
                .AssertReply(AssertContains(TicketResponses.TicketCreated, null, CardStrings.TicketUpdateClose))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task CreateActionTest()
        {
            var navigate = new Dictionary<string, string>
            {
                { "Navigate", string.Empty }
            };

            await this.GetSkillTestFlow()
                .Send(TicketCreateUtterances.CreateAction)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(SharedResponses.InputTitle))
                .Send(MockData.CreateTicketTitle)
                .AssertReply(AssertContains(KnowledgeResponses.ShowExistingToSolve))
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.Knowledge))
                .AssertReply(AssertStartsWith(KnowledgeResponses.IfExistingSolve, navigate))
                .Send(GeneralTestUtterances.Reject)
                .AssertReply(AssertContains(SharedResponses.InputDescription))
                .Send(MockData.CreateTicketDescription)
                .AssertReply(AssertStartsWith(SharedResponses.InputUrgency))
                .Send(NonLuisUtterances.CreateTicketUrgency)
                .AssertReply(AssertContains(TicketResponses.TicketCreated, null, CardStrings.TicketUpdateClose))
                .AssertReply(SkillActionEndMessage(true))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task CreateExistingSolveActionTest()
        {
            var navigate = new Dictionary<string, string>
            {
                { "Navigate", string.Empty }
            };

            await this.GetSkillTestFlow()
                .Send(TicketCreateUtterances.CreateAction)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertContains(SharedResponses.InputTitle))
                .Send(MockData.CreateTicketTitle)
                .AssertReply(AssertContains(KnowledgeResponses.ShowExistingToSolve))
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.Knowledge))
                .AssertReply(AssertStartsWith(KnowledgeResponses.IfExistingSolve, navigate))
                .Send(GeneralTestUtterances.Confirm)
                .AssertReply(SkillActionEndMessage(true))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task CreateWithTitleUrgencyDescriptionActionTest()
        {
            var confirmTitle = new Dictionary<string, string>
            {
                { "Title", MockData.CreateTicketTitle }
            };

            var navigate = new Dictionary<string, string>
            {
                { "Navigate", string.Empty }
            };

            var confirmUrgency = new Dictionary<string, string>
            {
                { "Urgency", MockData.CreateTicketUrgencyLevel.ToLocalizedString() }
            };

            var confirmDescription = new Dictionary<string, string>
            {
                { "Description", MockData.CreateTicketDescription }
            };

            await this.GetSkillTestFlow()
                .Send(TicketCreateUtterances.CreateWithTitleUrgencyDescriptionAction)
                .AssertReply(ShowAuth())
                .Send(MagicCode)
                .AssertReply(AssertStartsWith(SharedResponses.ConfirmTitle, confirmTitle))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(KnowledgeResponses.ShowExistingToSolve))
                .AssertReply(AssertContains(SharedResponses.ResultIndicator, null, CardStrings.Knowledge))
                .AssertReply(AssertStartsWith(KnowledgeResponses.IfExistingSolve, navigate))
                .Send(GeneralTestUtterances.Reject)
                .AssertReply(AssertStartsWith(SharedResponses.ConfirmDescription, confirmDescription))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertStartsWith(SharedResponses.ConfirmUrgency, confirmUrgency))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(TicketResponses.TicketCreated, null, CardStrings.TicketUpdateClose))
                .AssertReply(SkillActionEndMessage(true))
                .StartTestAsync();
        }
    }
}
