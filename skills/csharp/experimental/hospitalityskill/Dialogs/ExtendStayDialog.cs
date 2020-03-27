// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using HospitalitySkill.Models;
using HospitalitySkill.Responses.ExtendStay;
using HospitalitySkill.Responses.Shared;
using HospitalitySkill.Services;
using HospitalitySkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;

namespace HospitalitySkill.Dialogs
{
    public class ExtendStayDialog : HospitalityDialogBase
    {
        public ExtendStayDialog(
            IServiceProvider serviceProvider)
            : base(nameof(ExtendStayDialog), serviceProvider)
        {
            var extendStay = new WaterfallStep[]
            {
                HasCheckedOutAsync,
                CheckEntitiesAsync,
                ExtendDatePromptAsync,
                ConfirmExtentionPromptAsync,
                EndDialogAsync
            };

            AddDialog(new WaterfallDialog(nameof(ExtendStayDialog), extendStay));
            AddDialog(new ConfirmPrompt(DialogIds.CheckNumNights, ValidateCheckNumNightsPromptAsync));
            AddDialog(new DateTimePrompt(DialogIds.ExtendDatePrompt, ValidateDateAsync));
            AddDialog(new ConfirmPrompt(DialogIds.ConfirmExtendStay, ValidateConfirmExtensionAsync));
        }

        private async Task<DialogTurnResult> CheckEntitiesAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(sc.Context, () => new HospitalityUserSkillState(HotelService), cancellationToken);
            var convState = await StateAccessor.GetAsync(sc.Context, () => new HospitalitySkillState(), cancellationToken);
            var entities = convState?.LuisResult?.Entities;
            convState.UpdatedReservation = userState.UserReservation.Copy();

            if (entities == null)
            {
                return await sc.NextAsync(cancellationToken: cancellationToken);
            }

            // check for valid datetime entity
            if (entities.datetime != null && (entities.datetime[0].Type == "date" ||
                entities.datetime[0].Type == "datetime" || entities.datetime[0].Type == "daterange")
                && await DateValidationAsync(sc.Context, entities.datetime[0].Expressions, cancellationToken))
            {
                return await sc.NextAsync(cancellationToken: cancellationToken);
            }

            // check for valid number composite entity
            if (entities.NumNights?[0].HotelNights != null && entities.NumNights?[0].number[0] != null
                && await NumValidationAsync(sc.Context, entities.NumNights[0].number[0], cancellationToken))
            {
                return await sc.NextAsync(cancellationToken: cancellationToken);
            }

            // need clarification on input
            else if (entities.datetime == null && entities.number != null)
            {
                convState.NumberEntity = entities.number[0];

                var tokens = new Dictionary<string, object>
                {
                    { "Number", convState.NumberEntity.ToString() }
                };

                return await sc.PromptAsync(DialogIds.CheckNumNights, new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivity(ExtendStayResponses.ConfirmAddNights, tokens)
                }, cancellationToken);
            }

            // trying to request late check out time
            else if (convState.IsAction == false && entities.datetime != null && (entities.datetime[0].Type == "time" || entities.datetime[0].Type == "timerange"))
            {
                return await sc.ReplaceDialogAsync(nameof(LateCheckOutDialog), cancellationToken: cancellationToken);
            }

            return await sc.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<bool> ValidateCheckNumNightsPromptAsync(PromptValidatorContext<bool> promptContext, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(promptContext.Context, () => new HospitalitySkillState(), cancellationToken);

            // confirm number of nights they want to extend by
            if (promptContext.Recognized.Succeeded && promptContext.Recognized.Value)
            {
                await NumValidationAsync(promptContext.Context, convState.NumberEntity, cancellationToken);
            }

            return await Task.FromResult(true);
        }

        private async Task<bool> NumValidationAsync(ITurnContext turnContext, double extraNights, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(turnContext, () => new HospitalityUserSkillState(HotelService), cancellationToken);
            var convState = await StateAccessor.GetAsync(turnContext, () => new HospitalitySkillState(), cancellationToken);

            if (extraNights >= 1)
            {
                // add entity number to the current check out date
                DateTime currentDate = DateTime.Parse(userState.UserReservation.CheckOutDate);
                convState.UpdatedReservation.CheckOutDate = currentDate.AddDays(extraNights).ToString(ReservationData.DateFormat);
                return await Task.FromResult(true);
            }

            await turnContext.SendActivityAsync(TemplateManager.GenerateActivity(ExtendStayResponses.NumberEntityError), cancellationToken);
            return await Task.FromResult(false);
        }

        private async Task<DialogTurnResult> ExtendDatePromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(sc.Context, () => new HospitalityUserSkillState(HotelService), cancellationToken);
            var convState = await StateAccessor.GetAsync(sc.Context, () => new HospitalitySkillState(), cancellationToken);

            // if new date hasnt been set yet
            if (userState.UserReservation.CheckOutDate == convState.UpdatedReservation.CheckOutDate)
            {
                // get extended reservation date
                return await sc.PromptAsync(DialogIds.ExtendDatePrompt, new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivity(ExtendStayResponses.ExtendDatePrompt),
                    RetryPrompt = TemplateManager.GenerateActivity(ExtendStayResponses.RetryExtendDate)
                }, cancellationToken);
            }

            return await sc.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<bool> ValidateDateAsync(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded && promptContext.Recognized.Value[0].Value != null)
            {
                // convert DateTimeResolution list to string list
                List<string> dateValues = new List<string>();
                foreach (var date in promptContext.Recognized.Value)
                {
                    dateValues.AddRange(date.Value.Split(' '));
                }

                return await DateValidationAsync(promptContext.Context, dateValues, cancellationToken);
            }

            return await Task.FromResult(false);
        }

        private async Task<bool> DateValidationAsync(ITurnContext turnContext, IReadOnlyList<string> dates, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(turnContext, () => new HospitalitySkillState(), cancellationToken);
            var userState = await UserStateAccessor.GetAsync(turnContext, () => new HospitalityUserSkillState(HotelService), cancellationToken);

            DateTime dateObject = new DateTime();
            bool dateIsEarly = false;
            string[] formats = { "XXXX-MM-dd", "yyyy-MM-dd" };
            foreach (var date in dates)
            {
                // try parse exact date format so it won't accept time inputs
                if (DateTime.TryParseExact(date, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateObject))
                {
                    if (dateObject > DateTime.Now && dateObject > DateTime.Parse(userState.UserReservation.CheckOutDate))
                    {
                        // get first future date that is formatted correctly
                        convState.UpdatedReservation.CheckOutDate = dateObject.ToString(ReservationData.DateFormat);
                        return await Task.FromResult(true);
                    }
                    else
                    {
                        dateIsEarly = true;
                    }
                }
            }

            // found correctly formatted date, but date is not after current check-out date
            if (dateIsEarly)
            {
                // same date as current check-out date
                if (dateObject.ToString(ReservationData.DateFormat) == userState.UserReservation.CheckOutDate)
                {
                    await turnContext.SendActivityAsync(TemplateManager.GenerateActivity(ExtendStayResponses.SameDayRequested), cancellationToken);
                }
                else
                {
                    var tokens = new Dictionary<string, object>
                    {
                        { "Date", userState.UserReservation.CheckOutDate }
                    };

                    await turnContext.SendActivityAsync(TemplateManager.GenerateActivity(ExtendStayResponses.NotFutureDateError, tokens), cancellationToken);
                }
            }

            return await Task.FromResult(false);
        }

        private async Task<DialogTurnResult> ConfirmExtentionPromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(sc.Context, () => new HospitalitySkillState(), cancellationToken);

            var tokens = new Dictionary<string, object>
            {
                { "Date", convState.UpdatedReservation.CheckOutDate }
            };

            // confirm reservation extension with user
            return await sc.PromptAsync(DialogIds.ConfirmExtendStay, new PromptOptions()
            {
                Prompt = TemplateManager.GenerateActivity(ExtendStayResponses.ConfirmExtendStay, tokens),
                RetryPrompt = TemplateManager.GenerateActivity(ExtendStayResponses.RetryConfirmExtendStay, tokens)
            }, cancellationToken);
        }

        private async Task<bool> ValidateConfirmExtensionAsync(PromptValidatorContext<bool> promptContext, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(promptContext.Context, () => new HospitalitySkillState(), cancellationToken);
            var userState = await UserStateAccessor.GetAsync(promptContext.Context, () => new HospitalityUserSkillState(HotelService), cancellationToken);

            if (promptContext.Recognized.Succeeded)
            {
                bool response = promptContext.Recognized.Value;
                if (response)
                {
                    // TODO process requesting reservation extension
                    userState.UserReservation.CheckOutDate = convState.UpdatedReservation.CheckOutDate;

                    // set new checkout date in hotel service
                    HotelService.UpdateReservationDetails(userState.UserReservation);
                }

                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }

        private async Task<DialogTurnResult> EndDialogAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(sc.Context, () => new HospitalitySkillState(), cancellationToken);
            var userState = await UserStateAccessor.GetAsync(sc.Context, () => new HospitalityUserSkillState(HotelService), cancellationToken);

            if (userState.UserReservation.CheckOutDate == convState.UpdatedReservation.CheckOutDate)
            {
                var tokens = new Dictionary<string, object>
                {
                    { "Date", userState.UserReservation.CheckOutDate }
                };

                var cardData = userState.UserReservation;
                cardData.Title = TemplateManager.GetString(HospitalityStrings.UpdateReservation);

                // check out date moved confirmation
                var reply = TemplateManager.GenerateActivity(ExtendStayResponses.ExtendStaySuccess, new Card(GetCardName(sc.Context, "ReservationDetails"), cardData), tokens);
                await sc.Context.SendActivityAsync(reply, cancellationToken);

                return await sc.EndDialogAsync(await CreateSuccessActionResultAsync(sc.Context, cancellationToken), cancellationToken);
            }

            return await sc.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private static class DialogIds
        {
            public const string ExtendDatePrompt = "extendDatePrompt";
            public const string ConfirmExtendStay = "confirmExtendStay";
            public const string CheckNumNights = "checkNumNights";
        }
    }
}
