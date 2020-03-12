// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HospitalitySkill.Services;
using Luis;
using Microsoft.Bot.Builder;
using Newtonsoft.Json;
using static Luis.HospitalityLuis._Entities;

namespace HospitalitySkill.Models.ActionDefinitions
{
    public class RoomServiceInput : IActionInput
    {
        [JsonProperty("menu")]
        public string Menu { get; set; }

        [JsonProperty("food")]
        public Food[] Food { get; set; }

        public async Task Process(ITurnContext context, IStatePropertyAccessor<HospitalitySkillState> stateAccessor, IStatePropertyAccessor<HospitalityUserSkillState> userStateAccessor, IHotelService hotelService, CancellationToken cancellationToken)
        {
            var state = await stateAccessor.GetAsync(context, () => new HospitalitySkillState());
            state.LuisResult = new HospitalityLuis
            {
                Entities = new HospitalityLuis._Entities(),
            };

            if (!string.IsNullOrEmpty(Menu))
            {
                state.LuisResult.Entities.Menu = new string[][] { new string[] { Menu } };
            }

            if (Food != null && Food.Length > 0)
            {
                state.LuisResult.Entities.FoodRequest = Food.Select(food => new FoodRequestClass
                {
                    number = new double[] { food.Number },
                    Food = new string[] { food.Name },
                    SpecialRequest = string.IsNullOrEmpty(food.SpecialRequest) ? null : new string[] { food.SpecialRequest },
                }).ToArray();
            }
        }
    }
}
