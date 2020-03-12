// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Threading.Tasks;
using HospitalitySkill.Models;
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
    public class LateCheckOutFlowTests : HospitalitySkillTestBase
    {
        [TestMethod]
        public async Task LateCheckOutTest()
        {
            var tokens = new Dictionary<string, string>
            {
                { "Time", HotelService.LateTime.ToString(ReservationData.TimeFormat) },
                { "Date", CheckInDate.AddDays(HotelService.StayDays).ToString(ReservationData.DateFormat) }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(LateCheckOutUtterances.LateCheckOut)
                .AssertReply(AssertContains(LateCheckOutResponses.CheckAvailability))
                .AssertReply(AssertStartsWith(LateCheckOutResponses.MoveCheckOutPrompt, tokens))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(LateCheckOutResponses.MoveCheckOutSuccess, tokens, CardStrings.ReservationDetails))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task LateCheckOutWithTimeTest()
        {
            var tokens = new StringDictionary
            {
                { "Time", LateCheckOutUtterances.Time.ToString(ReservationData.TimeFormat) },
                { "Date", CheckInDate.AddDays(HotelService.StayDays).ToString(ReservationData.DateFormat) }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(LateCheckOutUtterances.LateCheckOutWithTime)
                .AssertReply(AssertContains(LateCheckOutResponses.CheckAvailability))
                .AssertReply(AssertStartsWith(LateCheckOutResponses.MoveCheckOutPrompt, tokens))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(LateCheckOutResponses.MoveCheckOutSuccess, tokens, CardStrings.ReservationDetails))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task LateCheckOutWithExceededTimeTest()
        {
            var tokens = new Dictionary<string, string>
            {
                { "Time", HotelService.LateTime.ToString(ReservationData.TimeFormat) },
                { "Date", CheckInDate.AddDays(HotelService.StayDays).ToString(ReservationData.DateFormat) }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(LateCheckOutUtterances.LateCheckOutWithExceededTime)
                .AssertReply(AssertContains(LateCheckOutResponses.CheckAvailability))
                .AssertReply(AssertStartsWith(LateCheckOutResponses.MoveCheckOutPrompt, tokens))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(LateCheckOutResponses.MoveCheckOutSuccess, tokens, CardStrings.ReservationDetails))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task LateCheckOutAndHelpTest()
        {
            var tokens = new Dictionary<string, string>
            {
                { "Time", HotelService.LateTime.ToString(ReservationData.TimeFormat) },
                { "Date", CheckInDate.AddDays(HotelService.StayDays).ToString(ReservationData.DateFormat) }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(LateCheckOutUtterances.LateCheckOut)
                .AssertReply(AssertContains(LateCheckOutResponses.CheckAvailability))
                .AssertReply(AssertStartsWith(LateCheckOutResponses.MoveCheckOutPrompt, tokens))
                .Send(GeneralTestUtterances.Help)
                .AssertReply(AssertContains(MainResponses.HelpMessage))
                .AssertReply(AssertStartsWith(LateCheckOutResponses.MoveCheckOutPrompt, tokens))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(LateCheckOutResponses.MoveCheckOutSuccess, tokens, CardStrings.ReservationDetails))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task LateCheckOutAndCancelTest()
        {
            var tokens = new Dictionary<string, string>
            {
                { "Time", HotelService.LateTime.ToString(ReservationData.TimeFormat) },
                { "Date", CheckInDate.AddDays(HotelService.StayDays).ToString(ReservationData.DateFormat) }
            };

            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .Send(LateCheckOutUtterances.LateCheckOut)
                .AssertReply(AssertContains(LateCheckOutResponses.CheckAvailability))
                .AssertReply(AssertStartsWith(LateCheckOutResponses.MoveCheckOutPrompt, tokens))
                .Send(GeneralTestUtterances.Cancel)
                .AssertReply(AssertContains(MainResponses.CancelMessage))
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task LateCheckOutActionTest()
        {
            var tokens = new StringDictionary
            {
                { "Time", HotelService.LateTime.ToString(ReservationData.TimeFormat) },
                { "Date", CheckInDate.AddDays(HotelService.StayDays).ToString(ReservationData.DateFormat) }
            };

            await this.GetSkillTestFlow()
                .Send(LateCheckOutUtterances.LateCheckOutAction)
                .AssertReply(AssertContains(LateCheckOutResponses.CheckAvailability))
                .AssertReply(AssertStartsWith(LateCheckOutResponses.MoveCheckOutPrompt, tokens))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(LateCheckOutResponses.MoveCheckOutSuccess, tokens, CardStrings.ReservationDetails))
                .AssertReply(SkillActionEndMessage(true))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task LateCheckOutWithTimeActionTest()
        {
            var tokens = new StringDictionary
            {
                { "Time", LateCheckOutUtterances.Time.ToString(ReservationData.TimeFormat) },
                { "Date", CheckInDate.AddDays(HotelService.StayDays).ToString(ReservationData.DateFormat) }
            };

            await this.GetSkillTestFlow()
                .Send(LateCheckOutUtterances.LateCheckOutWithTimeAction)
                .AssertReply(AssertContains(LateCheckOutResponses.CheckAvailability))
                .AssertReply(AssertStartsWith(LateCheckOutResponses.MoveCheckOutPrompt, tokens))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(LateCheckOutResponses.MoveCheckOutSuccess, tokens, CardStrings.ReservationDetails))
                .AssertReply(SkillActionEndMessage(true))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task LateCheckOutWithExceededTimeActionTest()
        {
            var tokens = new StringDictionary
            {
                { "Time", HotelService.LateTime.ToString(ReservationData.TimeFormat) },
                { "Date", CheckInDate.AddDays(HotelService.StayDays).ToString(ReservationData.DateFormat) }
            };

            await this.GetSkillTestFlow()
                .Send(LateCheckOutUtterances.LateCheckOutWithExceededTimeAction)
                .AssertReply(AssertContains(LateCheckOutResponses.CheckAvailability))
                .AssertReply(AssertStartsWith(LateCheckOutResponses.MoveCheckOutPrompt, tokens))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(LateCheckOutResponses.MoveCheckOutSuccess, tokens, CardStrings.ReservationDetails))
                .AssertReply(SkillActionEndMessage(true))
                .StartTestAsync();
        }
    }
}
