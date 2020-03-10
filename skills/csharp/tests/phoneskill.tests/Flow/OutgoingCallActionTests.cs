// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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

        private Activity OutgoingCallAction_PromptForContactName { get; } = new Activity()
        {
            Type = ActivityTypes.Event,
            Name = OutgoingCallActionName,
        };

        private Activity OutgoingCallAction_ContactNameAndPhoneNumber { get; } = new Activity()
        {
            Type = ActivityTypes.Event,
            Name = OutgoingCallActionName,
            Value = JObject.FromObject(new OutgoingCallRequest()
            {
                ContactPerson = "Bob Botter",
                PhoneNumber = "555 666 6666"
            })
        };

        private Activity OutgoingCallAction_PhoneNumber { get; } = new Activity()
        {
            Type = ActivityTypes.Event,
            Name = OutgoingCallActionName,
            Value = JObject.FromObject(new OutgoingCallRequest()
            {
                PhoneNumber = "555 666 6666"
            })
        };

        private Activity OutgoingCallAction_ContactName { get; } = new Activity()
        {
            Type = ActivityTypes.Event,
            Name = OutgoingCallActionName,
            Value = JObject.FromObject(new OutgoingCallRequest()
            {
                ContactPerson = "Bob Botter"
            })
        };

        private Activity OutgoingCallAction_ContactNameNotFound { get; } = new Activity()
        {
            Type = ActivityTypes.Event,
            Name = OutgoingCallActionName,
            Value = JObject.FromObject(new OutgoingCallRequest()
            {
                ContactPerson = "qqq"
            })
        };

        private Activity OutgoingCallAction_ContactNameNoPhoneNumber { get; } = new Activity()
        {
            Type = ActivityTypes.Event,
            Name = OutgoingCallActionName,
            Value = JObject.FromObject(new OutgoingCallRequest()
            {
                ContactPerson = "Christina Botter"
            })
        };

        [TestMethod]
        public async Task Test_OutgoingCall_Action_PromptForContactName()
        {
            await GetSkillTestFlow()
               .Send(OutgoingCallAction_PromptForContactName)
               .AssertReply(Message(OutgoingCallResponses.RecipientPrompt))
               .Send(OutgoingCallUtterances.RecipientContactName)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new StringDictionary()
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
            await this.GetSkillTestFlow()
                .Send(OutgoingCallAction_ContactNameAndPhoneNumber)
                .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new StringDictionary()
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
            await GetSkillTestFlow()
               .Send(OutgoingCallAction_PhoneNumber)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new StringDictionary()
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
            await GetSkillTestFlow()
               .Send(OutgoingCallAction_ContactName)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new StringDictionary()
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
            await GetSkillTestFlow()
               .Send(OutgoingCallAction_ContactNameNotFound)
               .AssertReply(Message(OutgoingCallResponses.ContactNotFound, new StringDictionary()
               {
                   { "contactName", "qqq" },
               }))
               .AssertReply(Message(OutgoingCallResponses.RecipientPrompt))
               .Send(OutgoingCallUtterances.RecipientContactName)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new StringDictionary()
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
            await GetSkillTestFlow()
               .Send(OutgoingCallAction_ContactNameNoPhoneNumber)
               .AssertReply(Message(OutgoingCallResponses.ContactHasNoPhoneNumber, new StringDictionary()
               {
                   { "contact", "Christina Botter" },
               }))
               .AssertReply(Message(OutgoingCallResponses.RecipientPrompt))
               .Send(OutgoingCallUtterances.OutgoingCallNoEntities) // Test that "Christina Botter" was completely removed from the state.
               .AssertReply(Message(OutgoingCallResponses.RecipientPrompt))
               .Send(OutgoingCallUtterances.RecipientContactName)
               .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new StringDictionary()
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
