// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using HospitalitySkill.Services;
using Microsoft.Bot.Builder;

namespace HospitalitySkill.Models.ActionDefinitions
{
    public interface IActionInput
    {
        Task Process(ITurnContext context, IStatePropertyAccessor<HospitalitySkillState> stateAccessor, IStatePropertyAccessor<HospitalityUserSkillState> userStateAccessor, IHotelService hotelService, CancellationToken cancellationToken);
    }
}
