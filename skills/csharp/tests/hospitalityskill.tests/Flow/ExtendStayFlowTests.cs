// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using HospitalitySkill.Models;
using HospitalitySkill.Responses.ExtendStay;
using HospitalitySkill.Responses.LateCheckOut;
using HospitalitySkill.Responses.Main;
using HospitalitySkill.Services;
using HospitalitySkill.Tests.Flow.Strings;
using HospitalitySkill.Tests.Flow.Utterances;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HospitalitySkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class ExtendStayFlowTests : HospitalitySkillTestBase
    {
        [TestMethod]
        public async Task ExtendStayTest()
        {
            var extendDate = CheckInDate + TimeSpan.FromDays(HotelService.StayDays + ExtendStayUtterances.Number);

            var tokens = new Dictionary<string, object>
            {
                { "Date", extendDate.ToString(ReservationData.DateFormat) }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(ExtendStayUtterances.ExtendStay)
                .AssertReply(AssertContains(ExtendStayResponses.ExtendDatePrompt))
                .Send(extendDate.ToString())
                .AssertReply(AssertStartsWith(ExtendStayResponses.ConfirmExtendStay, tokens))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(ExtendStayResponses.ExtendStaySuccess, tokens, CardStrings.ReservationDetails))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task ExtendStayWithDateTest()
        {
            var extendDate = CheckInDate + TimeSpan.FromDays(HotelService.StayDays + ExtendStayUtterances.Number);

            var tokens = new Dictionary<string, object>
            {
                { "Date", extendDate.ToString(ReservationData.DateFormat) }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(ExtendStayUtterances.ExtendStayWithDate)
                .AssertReply(AssertStartsWith(ExtendStayResponses.ConfirmExtendStay, tokens))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(ExtendStayResponses.ExtendStaySuccess, tokens, CardStrings.ReservationDetails))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task ExtendStayWithNumNightsTest()
        {
            var extendDate = CheckInDate + TimeSpan.FromDays(HotelService.StayDays + ExtendStayUtterances.Number);

            var tokens = new Dictionary<string, object>
            {
                { "Date", extendDate.ToString(ReservationData.DateFormat) }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(ExtendStayUtterances.ExtendStayWithNumNights)
                .AssertReply(AssertStartsWith(ExtendStayResponses.ConfirmExtendStay, tokens))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(ExtendStayResponses.ExtendStaySuccess, tokens, CardStrings.ReservationDetails))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task ExtendStayWithTimeTest()
        {
            var tokens = new Dictionary<string, object>
            {
                { "Time", LateCheckOutUtterances.Time.ToString(ReservationData.TimeFormat) },
                { "Date", CheckInDate.AddDays(HotelService.StayDays).ToString(ReservationData.DateFormat) }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(ExtendStayUtterances.ExtendStayWithTime)
                .AssertReply(AssertContains(LateCheckOutResponses.CheckAvailability))
                .AssertReply(AssertStartsWith(LateCheckOutResponses.MoveCheckOutPrompt, tokens))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(LateCheckOutResponses.MoveCheckOutSuccess, tokens, CardStrings.ReservationDetails))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task ExtendStayActionTest()
        {
            var extendDate = CheckInDate + TimeSpan.FromDays(HotelService.StayDays + ExtendStayUtterances.Number);

            var tokens = new Dictionary<string, object>
            {
                { "Date", extendDate.ToString(ReservationData.DateFormat) }
            };

            await this.GetSkillTestFlow()
                .Send(ExtendStayUtterances.ExtendStayAction)
                .AssertReply(AssertContains(ExtendStayResponses.ExtendDatePrompt))
                .Send(extendDate.ToString())
                .AssertReply(AssertStartsWith(ExtendStayResponses.ConfirmExtendStay, tokens))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(ExtendStayResponses.ExtendStaySuccess, tokens, CardStrings.ReservationDetails))
                .AssertReply(SkillActionEndMessage(true))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task ExtendStayWithDateActionTest()
        {
            var extendDate = CheckInDate + TimeSpan.FromDays(HotelService.StayDays + ExtendStayUtterances.Number);

            var tokens = new Dictionary<string, object>
            {
                { "Date", extendDate.ToString(ReservationData.DateFormat) }
            };

            await this.GetSkillTestFlow()
                .Send(ExtendStayUtterances.ExtendStayWithDateAction)
                .AssertReply(AssertStartsWith(ExtendStayResponses.ConfirmExtendStay, tokens))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(ExtendStayResponses.ExtendStaySuccess, tokens, CardStrings.ReservationDetails))
                .AssertReply(SkillActionEndMessage(true))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task ExtendStayThenTimeActionTest()
        {
            var extendDate = CheckInDate + TimeSpan.FromDays(HotelService.StayDays + ExtendStayUtterances.Number);

            var tokens = new Dictionary<string, object>
            {
                { "Date", extendDate.ToString(ReservationData.DateFormat) }
            };

            await this.GetSkillTestFlow()
                .Send(ExtendStayUtterances.ExtendStayAction)
                .AssertReply(AssertContains(ExtendStayResponses.ExtendDatePrompt))
                .Send(LateCheckOutUtterances.Time.ToString())
                .AssertReply(AssertContains(ExtendStayResponses.RetryExtendDate))
                .Send(extendDate.ToString())
                .AssertReply(AssertStartsWith(ExtendStayResponses.ConfirmExtendStay, tokens))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(ExtendStayResponses.ExtendStaySuccess, tokens, CardStrings.ReservationDetails))
                .AssertReply(SkillActionEndMessage(true))
                .StartTestAsync();
        }
    }
}
