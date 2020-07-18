using AdaptiveCards;
using ITSMSkill.Dialogs.Teams.TicketTaskModule;
using ITSMSkill.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ITSMSkill.Tests.Flow.TaskModuleUT
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class ITSMTaskModuleTests : SkillTestBase
    {
        [TestMethod]
        public async Task CreateTicketTaskModuleGetUserInputCard()
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();
            adapter.AddUserToken(AuthenticationProvider, adapter.Conversation.ChannelId, adapter.Conversation.User.Id, TestToken, MagicCode);

            // TaskModule Activity For Fetch
            var taskFetch = "{\r\n  \"data\": {\r\n    \"data\": {\r\n      \"TaskModuleFlowType\": \"CreateTicket_Form\",\r\n      \"Submit\": false\r\n    },\r\n    \"type\": \"task / fetch\"\r\n  },\r\n  \"context\": {\r\n    \"theme\": \"dark\"\r\n  }\r\n}";
            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "task/fetch",
                Value = JObject.Parse(taskFetch)
            };

            var turnContext = new TurnContext(adapter, activity);

            var teamsImplementation = new CreateTicketTeamsImplementation(sp);

            TaskModuleContinueResponse response = await teamsImplementation.OnTeamsTaskModuleFetchAsync(turnContext, CancellationToken.None);
            Assert.IsNotNull(response);

            Assert.AreEqual("Create Incident", response.Value.Title);
            var attachment = response.Value.Card;
            Assert.IsNotNull(attachment);
            var adaptiveCard = (AdaptiveCard)attachment.Content;
            Assert.IsNotNull(adaptiveCard);
            Assert.AreEqual(adaptiveCard.Id, "GetUserInput");

            // TODO: Add more validation steps on AdaptiveCard
        }

        [TestMethod]
        public async Task CreateTicketTaskModuleSubmitUserResponse()
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();
            var conversationState = sp.GetService<ConversationState>();
            var stateAccessor = conversationState.CreateProperty<SkillState>(nameof(SkillState));
            var skillState = new SkillState();
            skillState.AccessTokenResponse = new TokenResponse { Token = "Test" };

            // TaskModule Activity For Submit
            var taskSubmit = "{\r\n  \"data\": {\r\n    \"msteams\": {\r\n      \"type\": \"task/fetch\"\r\n    },\r\n    \"data\": {\r\n      \"TaskModuleFlowType\": \"CreateTicket_Form\",\r\n      \"Submit\": true\r\n    },\r\n    \"IncidentTitle\": \"Test15\",\r\n    \"IncidentDescription\": \"Test15\",\r\n    \"IncidentUrgency\": \"Medium\"\r\n  },\r\n  \"context\": {\r\n    \"theme\": \"dark\"\r\n  }\r\n}";
            var activity = new Activity
            {
                ChannelId = "test",
                Conversation = new ConversationAccount { Id = "Test" },
                Type = ActivityTypes.Invoke,
                Name = "task/fetch",
                Value = JObject.Parse(taskSubmit)
            };

            var turnContext = new TurnContext(adapter, activity);
            await stateAccessor.SetAsync(turnContext, skillState, CancellationToken.None);

            var teamsImplementation = new CreateTicketTeamsImplementation(sp);

            var response = await teamsImplementation.OnTeamsTaskModuleSubmitAsync(turnContext, CancellationToken.None);
            Assert.IsNotNull(response);
            Assert.AreEqual("Incident Created", response.Value.Title);
            var attachment = response.Value.Card;
            Assert.IsNotNull(attachment);
            var adaptiveCard = (AdaptiveCard)attachment.Content;
            Assert.IsNotNull(adaptiveCard);
            Assert.AreEqual(adaptiveCard.Id, "IncidentResponseCard");

            // TODO: Add more validation steps on AdaptiveCard
        }

        [TestMethod]
        public async Task UpdateTicketTaskModuleGetUserInputCard()
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();
            adapter.AddUserToken(AuthenticationProvider, adapter.Conversation.ChannelId, adapter.Conversation.User.Id, TestToken, MagicCode);

            // TaskModule Activity For Fetch
            var taskFetch = "{\r\n  \"data\": {\r\n    \"data\": {\r\n      \"TaskModuleFlowType\": \"UpdateTicket_Form\",\r\n      \"FlowData\": {\r\n       \"IncidentDetails\": {\r\n          \"Id\": \"120874\",\r\n          \"Title\": \"Test\",\r\n          \"Description\": \"Test\",\r\n          \"Urgency\": 1,\r\n          \"State\": 1,\r\n          \"OpenedTime\": \"2020-04-30T14:29:44.4485304Z\",\r\n          \"Number\": \"120874\"\r\n        }\r\n      },\r\n      \"Submit\": true\r\n    },\r\n    \"type\": \"task / fetch\"\r\n  },\r\n  \"context\": {\r\n    \"theme\": \"dark\"\r\n  }\r\n}";

            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "task/fetch",
                Value = JObject.Parse(taskFetch)
            };

            var turnContext = new TurnContext(adapter, activity);

            var teamsImplementation = new UpdateTicketTeamsImplementation(sp);

            TaskModuleContinueResponse response = await teamsImplementation.OnTeamsTaskModuleFetchAsync(turnContext, CancellationToken.None);
            Assert.IsNotNull(response);

            Assert.AreEqual("Update Incident", response.Value.Title);
            var attachment = response.Value.Card;
            Assert.IsNotNull(attachment);
            var adaptiveCard = (AdaptiveCard)attachment.Content;
            Assert.IsNotNull(adaptiveCard);
            Assert.AreEqual(adaptiveCard.Id, "UpdateAdaptiveCard");

            // TODO: Add more validation steps on AdaptiveCard
        }

        [TestMethod]
        public async Task UpdateTicketTaskModuleSubmitUserResponse()
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();
            var conversationState = sp.GetService<ConversationState>();
            var stateAccessor = conversationState.CreateProperty<SkillState>(nameof(SkillState));
            var skillState = new SkillState();
            skillState.AccessTokenResponse = new TokenResponse { Token = "Test" };

            // TaskModule Activity For Submit To Get New User Input
            var taskSubmit = "{\r\n  \"data\": {\r\n    \"msteams\": {\r\n      \"type\": \"task/submit\"\r\n    },\r\n    \"data\": {\r\n      \"TaskModuleFlowType\": \"UpdateTicket_Form\",\r\n     \"FlowData\": {\r\n       \"IncidentDetails\": {\r\n          \"Id\": \"MockCreateTicketId\",\r\n          \"Title\": \"Test\",\r\n          \"Description\": \"Test\",\r\n          \"Urgency\": 1,\r\n          \"State\": 1,\r\n          \"OpenedTime\": \"2020-04-30T14:29:44.4485304Z\",\r\n          \"Number\": \"MockCreateTicketId\"\r\n        }\r\n      },\r\n    \"Submit\": true\r\n    },\r\n    \"IncidentTitle\": \"Test15\",\r\n    \"IncidentDescription\": \"Test15\",\r\n    \"IncidentUrgency\": \"Medium\"\r\n  },\r\n  \"context\": {\r\n    \"theme\": \"dark\"\r\n  }\r\n}";
            var activity = new Activity
            {
                ChannelId = "test",
                Conversation = new ConversationAccount { Id = "Test" },
                Type = ActivityTypes.Invoke,
                Name = "task/fetch",
                Value = JObject.Parse(taskSubmit)
            };

            var turnContext = new TurnContext(adapter, activity);
            await stateAccessor.SetAsync(turnContext, skillState, CancellationToken.None);

            var teamsImplementation = new UpdateTicketTeamsImplementation(sp);

            var response = await teamsImplementation.OnTeamsTaskModuleSubmitAsync(turnContext, CancellationToken.None);
            Assert.IsNotNull(response);
            Assert.AreEqual("Incident Updated", response.Value.Title);
            var attachment = response.Value.Card;
            Assert.IsNotNull(attachment);
            var adaptiveCard = (AdaptiveCard)attachment.Content;
            Assert.IsNotNull(adaptiveCard);
            Assert.AreEqual(adaptiveCard.Id, "IncidentResponseCard");

            // TODO: Add more validation steps on AdaptiveCard
        }

        [TestMethod]
        public async Task DeleteTicketTaskModuleGetUserInputCard()
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();
            adapter.AddUserToken(AuthenticationProvider, adapter.Conversation.ChannelId, adapter.Conversation.User.Id, TestToken, MagicCode);

            // TaskModule Activity For Fetch
            var taskFetch = "{\r\n  \"data\": {\r\n    \"data\": {\r\n      \"TaskModuleFlowType\": \"DeleteTicket_Form\",\r\n      \"FlowData\": {\r\n       \"IncidentId\": \"120874\"\r\n },\r\n      \"Submit\": true\r\n    },\r\n    \"type\": \"task / fetch\"\r\n  },\r\n  \"context\": {\r\n    \"theme\": \"dark\"\r\n  }\r\n}";

            var activity = new Activity
            {
                Type = ActivityTypes.Invoke,
                Name = "task/fetch",
                Value = JObject.Parse(taskFetch),
                ChannelId = "msteams"
            };

            var turnContext = new TurnContext(adapter, activity);

            var teamsImplementation = new DeleteTicketTeamsImplementation(sp);

            TaskModuleContinueResponse response = await teamsImplementation.OnTeamsTaskModuleFetchAsync(turnContext, CancellationToken.None);
            Assert.IsNotNull(response);

            Assert.AreEqual("DeleteIncident", response.Value.Title);
            var attachment = response.Value.Card;
            Assert.IsNotNull(attachment);
            var adaptiveCard = (AdaptiveCard)attachment.Content;
            Assert.IsNotNull(adaptiveCard);
            Assert.AreEqual(adaptiveCard.Id, "DeleteTicketAdaptiveCard");

            // TODO: Add more validation steps on AdaptiveCard
        }

        [TestMethod]
        public async Task DeleteTicketTaskModuleSubmitUserInputCard()
        {
            var sp = Services.BuildServiceProvider();
            var adapter = sp.GetService<TestAdapter>();
            var conversationState = sp.GetService<ConversationState>();
            var stateAccessor = conversationState.CreateProperty<SkillState>(nameof(SkillState));
            var skillState = new SkillState();
            skillState.AccessTokenResponse = new TokenResponse { Token = "Test" };

            // TaskModule Activity For Submit
            var taskSubmit = "{\r\n  \"data\": {\r\n    \"msteams\": {\r\n      \"type\": \"task /fetch\"\r\n    },\r\n    \"data\": {\r\n      \"TaskModuleFlowType\": \"DeleteTicket_Form\",\r\n      \"FlowData\": {\r\n        \"IncidentId\": \"MockCreateTicketId\"\r\n      },\r\n      \"Submit\": true\r\n    },\r\n    \"IncidentCloseReason\": \"Test\"\r\n  },\r\n  \"context\": {\r\n    \"theme\": \"dark\"\r\n  }\r\n}";

            var activity = new Activity
            {
                ChannelId = "test",
                Conversation = new ConversationAccount { Id = "Test" },
                Type = ActivityTypes.Invoke,
                Name = "task/fetch",
                Value = JObject.Parse(taskSubmit)
            };

            var turnContext = new TurnContext(adapter, activity);
            await stateAccessor.SetAsync(turnContext, skillState, CancellationToken.None);

            var teamsImplementation = new DeleteTicketTeamsImplementation(sp);

            TaskModuleContinueResponse response = await teamsImplementation.OnTeamsTaskModuleSubmitAsync(turnContext, CancellationToken.None);
            Assert.IsNotNull(response);

            Assert.AreEqual("Incident Deleted", response.Value.Title);
            var attachment = response.Value.Card;
            Assert.IsNotNull(attachment);
            var adaptiveCard = (AdaptiveCard)attachment.Content;
            Assert.IsNotNull(adaptiveCard);
            Assert.AreEqual(adaptiveCard.Id, "IncidentResponseCard");

            // TODO: Add more validation steps on AdaptiveCard
        }
    }
}
