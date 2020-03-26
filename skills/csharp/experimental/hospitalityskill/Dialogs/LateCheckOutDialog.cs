// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using HospitalitySkill.Models;
using HospitalitySkill.Responses.LateCheckOut;
using HospitalitySkill.Responses.Shared;
using HospitalitySkill.Services;
using HospitalitySkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace HospitalitySkill.Dialogs
{
    public class LateCheckOutDialog : HospitalityDialogBase
    {
        public LateCheckOutDialog(
            IServiceProvider serviceProvider)
            : base(nameof(LateCheckOutDialog), serviceProvider)
        {
            var lateCheckOut = new WaterfallStep[]
            {
                HasCheckedOutAsync,
                LateCheckOutPromptAsync,
                EndDialogAsync
            };

            AddDialog(new WaterfallDialog(nameof(LateCheckOutDialog), lateCheckOut));
            AddDialog(new ConfirmPrompt(DialogIds.LateCheckOutPrompt, ValidateLateCheckOutAsync));
        }

        private async Task<DialogTurnResult> LateCheckOutPromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(sc.Context, () => new HospitalityUserSkillState(HotelService), cancellationToken);

            // already requested late check out
            if (userState.LateCheckOut)
            {
                var cardData = userState.UserReservation;
                cardData.Title = TemplateManager.GetString(HospitalityStrings.ReservationDetails);

                var reply = TemplateManager.GenerateActivity(LateCheckOutResponses.HasLateCheckOut, new Card(GetCardName(sc.Context, "ReservationDetails"), cardData), null);
                await sc.Context.SendActivityAsync(reply, cancellationToken);

                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }

            // TODO checking availability
            // simulate with time delay
            await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(LateCheckOutResponses.CheckAvailability), cancellationToken);
            await Task.Delay(1600);
            var lateTime = await HotelService.GetLateCheckOutAsync();

            var convState = await StateAccessor.GetAsync(sc.Context, () => new HospitalitySkillState(), cancellationToken);
            var entities = convState?.LuisResult?.Entities;
            if (entities != null && entities.datetime != null && (entities.datetime[0].Type == "time" || entities.datetime[0].Type == "timerange"))
            {
                // ISO 8601 https://docs.microsoft.com/en-us/azure/cognitive-services/luis/luis-reference-prebuilt-datetimev2?tabs=1-1%2C2-1%2C3-1%2C4-1%2C5-1%2C6-1
                var timexProperty = new TimexProperty();
                TimexParsing.ParseString(entities.datetime[0].Expressions[0], timexProperty);
                var preferedTime = new TimeSpan(timexProperty.Hour ?? 0, timexProperty.Minute ?? 0, timexProperty.Second ?? 0) + new TimeSpan((int)(timexProperty.Hours ?? 0), (int)(timexProperty.Minutes ?? 0), (int)(timexProperty.Seconds ?? 0));
                if (preferedTime < lateTime)
                {
                    lateTime = preferedTime;
                }
            }

            convState.UpdatedReservation = new ReservationData { CheckOutTimeData = lateTime };

            var tokens = new Dictionary<string, object>
            {
                { "Time", convState.UpdatedReservation.CheckOutTime },
            };

            return await sc.PromptAsync(DialogIds.LateCheckOutPrompt, new PromptOptions()
            {
                Prompt = TemplateManager.GenerateActivity(LateCheckOutResponses.MoveCheckOutPrompt, tokens),
                RetryPrompt = TemplateManager.GenerateActivity(LateCheckOutResponses.RetryMoveCheckOut),
            }, cancellationToken);
        }

        private async Task<bool> ValidateLateCheckOutAsync(PromptValidatorContext<bool> promptContext, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(promptContext.Context, () => new HospitalityUserSkillState(HotelService), cancellationToken);

            if (promptContext.Recognized.Succeeded)
            {
                bool response = promptContext.Recognized.Value;
                if (response)
                {
                    // TODO process late check out request here
                    userState.LateCheckOut = true;

                    var convState = await StateAccessor.GetAsync(promptContext.Context, () => new HospitalitySkillState(), cancellationToken);
                    userState.UserReservation.CheckOutTimeData = convState.UpdatedReservation.CheckOutTimeData;

                    // set new checkout in hotel service
                    HotelService.UpdateReservationDetails(userState.UserReservation);
                }

                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }

        private async Task<DialogTurnResult> EndDialogAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(sc.Context, () => new HospitalityUserSkillState(HotelService), cancellationToken);

            if (userState.LateCheckOut)
            {
                var tokens = new Dictionary<string, object>
                {
                    { "Time", userState.UserReservation.CheckOutTime },
                    { "Date", userState.UserReservation.CheckOutDate }
                };

                var cardData = userState.UserReservation;
                cardData.Title = TemplateManager.GetString(HospitalityStrings.UpdateReservation);

                // check out time moved confirmation
                var reply = TemplateManager.GenerateActivity(LateCheckOutResponses.MoveCheckOutSuccess, new Card(GetCardName(sc.Context, "ReservationDetails"), cardData), tokens);
                await sc.Context.SendActivityAsync(reply, cancellationToken);

                return await sc.EndDialogAsync(await CreateSuccessActionResultAsync(sc.Context, cancellationToken), cancellationToken);
            }

            return await sc.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private static class DialogIds
        {
            public const string LateCheckOutPrompt = "lateCheckOutPrompt";
        }
    }
}
