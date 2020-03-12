// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using HospitalitySkill.Services;
using Microsoft.Bot.Builder;
using Newtonsoft.Json;

namespace HospitalitySkill.Models.ActionDefinitions
{
    public class CheckOutInput : IActionInput
    {
        [JsonProperty("email")]
        public string Email { get; set; }

        public async Task Process(ITurnContext context, IStatePropertyAccessor<HospitalitySkillState> stateAccessor, IStatePropertyAccessor<HospitalityUserSkillState> userStateAccessor, IHotelService hotelService, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(Email))
            {
                var state = await userStateAccessor.GetAsync(context, () => new HospitalityUserSkillState(hotelService));
                state.Email = Email;
            }
        }
    }
}
