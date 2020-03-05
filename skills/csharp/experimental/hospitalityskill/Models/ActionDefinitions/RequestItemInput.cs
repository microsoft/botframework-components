// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using HospitalitySkill.Services;
using Microsoft.Bot.Builder;
using Newtonsoft.Json;

namespace HospitalitySkill.Models.ActionDefinitions
{
    public class RequestItemInput : IActionInput
    {
        [JsonProperty("items")]
        public Item[] Items { get; set; }

        public async Task Process(ITurnContext context, IStatePropertyAccessor<HospitalitySkillState> stateAccessor, IStatePropertyAccessor<HospitalityUserSkillState> userStateAccessor, IHotelService hotelService, CancellationToken cancellationToken)
        {
        }
    }
}
