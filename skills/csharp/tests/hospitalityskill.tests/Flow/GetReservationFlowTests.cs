// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using HospitalitySkill.Responses.GetReservation;
using HospitalitySkill.Responses.Main;
using HospitalitySkill.Tests.Flow.Strings;
using HospitalitySkill.Tests.Flow.Utterances;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HospitalitySkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class GetReservationFlowTests : HospitalitySkillTestBase
    {
        [TestMethod]
        public async Task GetReservationTest()
        {
            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .AssertReply(AssertContains(MainResponses.FirstPromptMessage))
                .Send(GetReservationUtterances.GetReservation)
                .AssertReply(AssertContains(GetReservationResponses.ShowReservationDetails, null, CardStrings.ReservationDetails))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task GetReservationActionTest()
        {
            await this.GetSkillTestFlow()
                .Send(GetReservationUtterances.GetReservationAction)
                .AssertReply(AssertContains(GetReservationResponses.ShowReservationDetails, null, CardStrings.ReservationDetails))
                .AssertReply(SkillActionEndMessage(true))
                .StartTestAsync();
        }
    }
}
