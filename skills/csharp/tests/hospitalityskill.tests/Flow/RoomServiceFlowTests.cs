// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;
using HospitalitySkill.Responses.Main;
using HospitalitySkill.Responses.RoomService;
using HospitalitySkill.Tests.Flow.Strings;
using HospitalitySkill.Tests.Flow.Utterances;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HospitalitySkill.Tests.Flow
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class RoomServiceFlowTests : HospitalitySkillTestBase
    {
        [TestMethod]
        public async Task RoomServiceTest()
        {
            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .AssertReply(AssertContains(MainResponses.FirstPromptMessage))
                .Send(RoomServiceUtterances.RoomService)
                .AssertReply(AssertContains(RoomServiceResponses.MenuPrompt, null, HeroCard.ContentType))
                .Send(RoomServiceUtterances.Breakfast)
                .AssertReply(AssertContains(null, null, CardStrings.MenuCard))
                .AssertReply(AssertContains(RoomServiceResponses.FoodOrder))
                .Send(RoomServiceUtterances.RoomServiceWithFood)
                .AssertReply(AssertContains(null, null, CardStrings.FoodOrderCard))
                .AssertReply(AssertContains(RoomServiceResponses.AddMore))
                .Send(NonLuisUtterances.No)
                .AssertReply(AssertStartsWith(RoomServiceResponses.ConfirmOrder))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(RoomServiceResponses.FinalOrderConfirmation))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task RoomServiceWithMenuTest()
        {
            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .AssertReply(AssertContains(MainResponses.FirstPromptMessage))
                .Send(RoomServiceUtterances.RoomServiceWithMenu)
                .AssertReply(AssertContains(null, null, CardStrings.MenuCard))
                .AssertReply(AssertContains(RoomServiceResponses.FoodOrder))
                .Send(RoomServiceUtterances.RoomServiceWithFood)
                .AssertReply(AssertContains(null, null, CardStrings.FoodOrderCard))
                .AssertReply(AssertContains(RoomServiceResponses.AddMore))
                .Send(NonLuisUtterances.No)
                .AssertReply(AssertStartsWith(RoomServiceResponses.ConfirmOrder))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(RoomServiceResponses.FinalOrderConfirmation))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task RoomServiceWithFoodTest()
        {
            await this.GetTestFlow()
                .Send(StartActivity)
                .AssertReply(AssertContains(MainResponses.WelcomeMessage))
                .AssertReply(AssertContains(MainResponses.FirstPromptMessage))
                .Send(RoomServiceUtterances.RoomServiceWithFood)
                .AssertReply(AssertContains(null, null, CardStrings.FoodOrderCard))
                .AssertReply(AssertContains(RoomServiceResponses.AddMore))
                .Send(NonLuisUtterances.No)
                .AssertReply(AssertStartsWith(RoomServiceResponses.ConfirmOrder))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(RoomServiceResponses.FinalOrderConfirmation))
                .AssertReply(ActionEndMessage())
                .StartTestAsync();
        }

        [TestMethod]
        public async Task RoomServiceTestAction()
        {
            await this.GetSkillTestFlow()
                .Send(RoomServiceUtterances.RoomServiceAction)
                .AssertReply(AssertContains(RoomServiceResponses.MenuPrompt, null, HeroCard.ContentType))
                .Send(RoomServiceUtterances.Breakfast)
                .AssertReply(AssertContains(null, null, CardStrings.MenuCard))
                .AssertReply(AssertContains(RoomServiceResponses.FoodOrder))
                .Send(RoomServiceUtterances.RoomServiceWithFood)
                .AssertReply(AssertContains(null, null, CardStrings.FoodOrderCard))
                .AssertReply(AssertContains(RoomServiceResponses.AddMore))
                .Send(NonLuisUtterances.No)
                .AssertReply(AssertStartsWith(RoomServiceResponses.ConfirmOrder))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(RoomServiceResponses.FinalOrderConfirmation))
                .AssertReply(SkillActionEndMessage(true))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task RoomServiceWithMenuActionTest()
        {
            await this.GetSkillTestFlow()
                .Send(RoomServiceUtterances.RoomServiceWithMenuAction)
                .AssertReply(AssertContains(null, null, CardStrings.MenuCard))
                .AssertReply(AssertContains(RoomServiceResponses.FoodOrder))
                .Send(RoomServiceUtterances.RoomServiceWithFood)
                .AssertReply(AssertContains(null, null, CardStrings.FoodOrderCard))
                .AssertReply(AssertContains(RoomServiceResponses.AddMore))
                .Send(NonLuisUtterances.No)
                .AssertReply(AssertStartsWith(RoomServiceResponses.ConfirmOrder))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(RoomServiceResponses.FinalOrderConfirmation))
                .AssertReply(SkillActionEndMessage(true))
                .StartTestAsync();
        }

        [TestMethod]
        public async Task RoomServiceWithFoodActionTest()
        {
            await this.GetSkillTestFlow()
                .Send(RoomServiceUtterances.RoomServiceWithFoodAction)
                .AssertReply(AssertContains(null, null, CardStrings.FoodOrderCard))
                .AssertReply(AssertContains(RoomServiceResponses.AddMore))
                .Send(NonLuisUtterances.No)
                .AssertReply(AssertStartsWith(RoomServiceResponses.ConfirmOrder))
                .Send(NonLuisUtterances.Yes)
                .AssertReply(AssertContains(RoomServiceResponses.FinalOrderConfirmation))
                .AssertReply(SkillActionEndMessage(true))
                .StartTestAsync();
        }
    }
}
