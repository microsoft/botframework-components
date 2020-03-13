﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
            BotSettings settings,
            BotServices services,
            LocaleTemplateManager templateManager,
            ConversationState conversationState,
            UserState userState,
            IHotelService hotelService,
            IBotTelemetryClient telemetryClient)
            : base(nameof(GetReservationDialog), settings, services, templateManager, conversationState, userState, hotelService, telemetryClient)
        {
            var getReservation = new WaterfallStep[]
            {
                HasCheckedOut,
                ShowReservation
            };

            HotelService = hotelService;

            AddDialog(new WaterfallDialog(nameof(GetReservationDialog), getReservation));
        }

        private async Task<DialogTurnResult> ShowReservation(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(sc.Context, () => new HospitalityUserSkillState(HotelService));
            var cardData = userState.UserReservation;
            cardData.Title = TemplateManager.GetString(HospitalityStrings.ReservationDetails);

            // send card with reservation details
            var reply = TemplateManager.GenerateActivity(GetReservationResponses.ShowReservationDetails, new Card(GetCardName(sc.Context, "ReservationDetails"), cardData), null);
            await sc.Context.SendActivityAsync(reply);
            return await sc.EndDialogAsync(await CreateSuccessActionResult(sc.Context));
        }
    }
}
