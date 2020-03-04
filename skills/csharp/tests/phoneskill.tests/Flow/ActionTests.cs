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

namespace PhoneSkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class ActionTests : PhoneSkillTestBase
    {
        private static string OutgoingCallActionName { get; } = "OutgoingCall";

        private static string OutgoingCallRequestPhoneNumber { get; } = "+1 234 567 8901";

        private static string OutgoingCallRequestContactName { get; } = "Megan";

        private Activity OutgoingCallAction { get; } = new Activity()
        {
            Type = ActivityTypes.Event,
            Name = OutgoingCallActionName,
            Value = JObject.FromObject(new OutgoingCallRequest()
            {
                ContactPerson = OutgoingCallRequestContactName,
                PhoneNumber = OutgoingCallRequestPhoneNumber
            })
        };

        [TestMethod]
        public async Task Test_OutgoingCall_Action()
        {
            await this.GetSkillTestFlow()
                .Send(OutgoingCallAction)
                .AssertReply(Message(OutgoingCallResponses.ExecuteCall, new StringDictionary()
                {
                    { "contactOrPhoneNumber", OutgoingCallRequestPhoneNumber },
                }))
                .AssertReply(OutgoingCallEvent(new OutgoingCall
                {
                    Number = OutgoingCallRequestPhoneNumber,
                }))
                .StartTestAsync();
        }
    }
}
