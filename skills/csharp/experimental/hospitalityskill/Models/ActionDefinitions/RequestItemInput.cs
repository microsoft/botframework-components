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
    public class RequestItemInput : IActionInput
    {
        [JsonProperty("items")]
        public Item[] Items { get; set; }

        public async Task Process(ITurnContext context, IStatePropertyAccessor<HospitalitySkillState> stateAccessor, IStatePropertyAccessor<HospitalityUserSkillState> userStateAccessor, IHotelService hotelService, CancellationToken cancellationToken)
        {
            if (Items != null && Items.Length > 0)
            {
                var state = await stateAccessor.GetAsync(context, () => new HospitalitySkillState());
                state.LuisResult = new HospitalityLuis
                {
                    Entities = new HospitalityLuis._Entities
                    {
                        ItemRequest = Items.Select((item) => new ItemRequestClass
                        {
                            Item = new string[] { item.Name },
                            number = new double[] { item.Number },
                        }).ToArray()
                    }
                };
            }
        }
    }
}
