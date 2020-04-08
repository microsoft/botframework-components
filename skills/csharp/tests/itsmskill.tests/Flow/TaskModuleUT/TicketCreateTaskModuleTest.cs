using AdaptiveCards;
using ITSMSkill.Dialogs.Teams.TicketTaskModule;
using ITSMSkill.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Schema;
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
    public class TicketCreateTaskModuleTest : SkillTestBase
    {
        [TestMethod]
        public async Task CreateTestTaskModuleGetUserInputCard()
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

            var response = await teamsImplementation.Handle(turnContext, CancellationToken.None);
            Assert.IsNotNull(response);
            Assert.AreEqual("GetUserInput", response.Task.TaskInfo.Title);
            var attachment = response.Task.TaskInfo.Card;
            Assert.IsNotNull(attachment);
            var adaptiveCard = (AdaptiveCard)attachment.Content;
            Assert.IsNotNull(adaptiveCard);
            Assert.AreEqual(adaptiveCard.Id, "GetUserInput");

            // TODO: Add more validation steps on AdaptiveCard
        }

        [TestMethod]
        public async Task CreateTestTaskModuleSubmitUserResponse()
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

            var response = await teamsImplementation.Handle(turnContext, CancellationToken.None);
            Assert.IsNotNull(response);
            Assert.AreEqual("IncidentAdded", response.Task.TaskInfo.Title);
            var attachment = response.Task.TaskInfo.Card;
            Assert.IsNotNull(attachment);
            var adaptiveCard = (AdaptiveCard)attachment.Content;
            Assert.IsNotNull(adaptiveCard);
            Assert.AreEqual(adaptiveCard.Id, "ResponseCard");

            // TODO: Add more validation steps on AdaptiveCard
        }
    }
}
