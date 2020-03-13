// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using PhoneSkill.Models;
using PhoneSkill.Models.Actions;
using PhoneSkill.Responses.Main;
using PhoneSkill.Responses.OutgoingCall;
using PhoneSkill.Tests.Flow.Utterances;
using PhoneSkill.Tests.TestDouble;

namespace PhoneSkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class OutgoingCallActionTests : PhoneSkillTestBase
    {
        private static string OutgoingCallActionName { get; } = "OutgoingCall";

        [TestMethod]
        public async Task Test_OutgoingCall_Action_PromptForContactName()
        {
            var activity = new Activity()
            {
                Type = ActivityTypes.Event,
                Name = OutgoingCallActionName,
            };

            await GetSkillTestFlow()
                .Send(activity)
                .AssertReply(Message(OutgoingCallResponses.RecipientPrompt))
                .Send(OutgoingCallUtterances.RecipientContactName)
                .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new Dictionary<string, object>()
                {
                    { "contactOrPhoneNumber", "Bob Botter" },
                }))
                .AssertReply(OutgoingCallEvent(new OutgoingCall
                {
                    Number = "555 666 6666",
                    Contact = StubContactProvider.BobBotter,
                }))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_Action_ContactNameAndPhoneNumber()
        {
            var activity = new Activity()
            {
                Type = ActivityTypes.Event,
                Name = OutgoingCallActionName,
                Value = JObject.FromObject(new OutgoingCallRequest()
                {
                    ContactPerson = "Bob Botter",
                    PhoneNumber = "555 666 6666"
                })
            };

            await this.GetSkillTestFlow()
                .Send(activity)
                .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new Dictionary<string, object>()
                {
                    { "contactOrPhoneNumber", "Bob Botter" },
                }))
                .AssertReply(OutgoingCallEvent(new OutgoingCall
                {
                    Number = "555 666 6666",
                    Contact = StubContactProvider.BobBotter
                }))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_Action_PhoneNumber()
        {
            var activity = new Activity()
            {
                Type = ActivityTypes.Event,
                Name = OutgoingCallActionName,
                Value = JObject.FromObject(new OutgoingCallRequest()
                {
                    PhoneNumber = "555 666 6666"
                })
            };

            await GetSkillTestFlow()
               .Send(activity)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "555 666 6666" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 666 6666",
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_Action_ContactName()
        {
            var activity = new Activity()
            {
                Type = ActivityTypes.Event,
                Name = OutgoingCallActionName,
                Value = JObject.FromObject(new OutgoingCallRequest()
                {
                    ContactPerson = "Bob Botter"
                })
            };

            await GetSkillTestFlow()
               .Send(activity)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Bob Botter" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 666 6666",
                   Contact = StubContactProvider.BobBotter,
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_Action_ContactNameNotFound()
        {
            var activity = new Activity()
            {
                Type = ActivityTypes.Event,
                Name = OutgoingCallActionName,
                Value = JObject.FromObject(new OutgoingCallRequest()
                {
                    ContactPerson = "qqq"
                })
            };

            await GetSkillTestFlow()
               .Send(activity)
               .AssertReply(Message(OutgoingCallResponses.ContactNotFound, new Dictionary<string, object>()
               {
                   { "contactName", "qqq" },
               }))
               .AssertReply(Message(OutgoingCallResponses.RecipientPrompt))
               .Send(OutgoingCallUtterances.RecipientContactName)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Bob Botter" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 666 6666",
                   Contact = StubContactProvider.BobBotter,
               }))
               .StartTestAsync();
        }

        [TestMethod]
        public async Task Test_OutgoingCall_Action_ContactNameNoPhoneNumber()
        {
            var activity = new Activity()
            {
                Type = ActivityTypes.Event,
                Name = OutgoingCallActionName,
                Value = JObject.FromObject(new OutgoingCallRequest()
                {
                    ContactPerson = "Christina Botter"
                })
            };

            await GetSkillTestFlow()
               .Send(activity)
               .AssertReply(Message(OutgoingCallResponses.ContactHasNoPhoneNumber, new Dictionary<string, object>()
               {
                   { "contact", "Christina Botter" },
               }))
               .AssertReply(Message(OutgoingCallResponses.RecipientPrompt))
               .Send(OutgoingCallUtterances.OutgoingCallNoEntities) // Test that "Christina Botter" was completely removed from the state.
               .AssertReply(Message(OutgoingCallResponses.RecipientPrompt))
               .Send(OutgoingCallUtterances.RecipientContactName)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new Dictionary<string, object>()
               {
                   { "contactOrPhoneNumber", "Bob Botter" },
               }))
               .AssertReply(OutgoingCallEvent(new OutgoingCall
               {
                   Number = "555 666 6666",
                   Contact = StubContactProvider.BobBotter,
               }))
               .StartTestAsync();
        }
    }
}
