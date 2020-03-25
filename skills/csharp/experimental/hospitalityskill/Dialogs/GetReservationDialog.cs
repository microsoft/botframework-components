// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using HospitalitySkill.Models;
using HospitalitySkill.Responses.GetReservation;
using HospitalitySkill.Responses.Shared;
using HospitalitySkill.Services;
using HospitalitySkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;

namespace HospitalitySkill.Dialogs
{
    public class GetReservationDialog : HospitalityDialogBase
    {
        public GetReservationDialog(
            IServiceProvider serviceProvider)
            : base(nameof(GetReservationDialog), serviceProvider)
        {
            var getReservation = new WaterfallStep[]
            {
                HasCheckedOutAsync,
                ShowReservationAsync
            };

            AddDialog(new WaterfallDialog(nameof(GetReservationDialog), getReservation));
        }

        private async Task<DialogTurnResult> ShowReservationAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(sc.Context, () => new HospitalityUserSkillState(HotelService), cancellationToken);
            var cardData = userState.UserReservation;
            cardData.Title = TemplateManager.GetString(HospitalityStrings.ReservationDetails);

            // send card with reservation details
            var reply = TemplateManager.GenerateActivity(GetReservationResponses.ShowReservationDetails, new Card(GetCardName(sc.Context, "ReservationDetails"), cardData), null);
            await sc.Context.SendActivityAsync(reply, cancellationToken);
            return await sc.EndDialogAsync(await CreateSuccessActionResultAsync(sc.Context, cancellationToken), cancellationToken);
        }
    }
}
