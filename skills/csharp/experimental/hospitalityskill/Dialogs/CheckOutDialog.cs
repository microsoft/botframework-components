// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using HospitalitySkill.Models;
using HospitalitySkill.Responses.CheckOut;
using HospitalitySkill.Services;
using HospitalitySkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;

namespace HospitalitySkill.Dialogs
{
    public class CheckOutDialog : HospitalityDialogBase
    {
        public CheckOutDialog(
            IServiceProvider serviceProvider)
            : base(nameof(CheckOutDialog), serviceProvider)
        {
            var checkOut = new WaterfallStep[]
            {
                HasCheckedOut,
                CheckOutPrompt,
                EmailPrompt,
                EndDialog
            };

            AddDialog(new WaterfallDialog(nameof(CheckOutDialog), checkOut));
            AddDialog(new ConfirmPrompt(DialogIds.CheckOutPrompt, ValidateCheckOutAsync));
            AddDialog(new TextPrompt(DialogIds.EmailPrompt, ValidateEmailAsync));
        }

        private async Task<DialogTurnResult> CheckOutPrompt(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            // confirm user wants to check out
            return await sc.PromptAsync(DialogIds.CheckOutPrompt, new PromptOptions()
            {
                Prompt = TemplateManager.GenerateActivity(CheckOutResponses.ConfirmCheckOut),
                RetryPrompt = TemplateManager.GenerateActivity(CheckOutResponses.RetryConfirmCheckOut),
            });
        }

        private async Task<bool> ValidateCheckOutAsync(PromptValidatorContext<bool> promptContext, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(promptContext.Context, () => new HospitalityUserSkillState(HotelService));

            if (promptContext.Recognized.Succeeded)
            {
                bool response = promptContext.Recognized.Value;
                if (response)
                {
                    // TODO process check out request here
                    // set checkout value
                    userState.CheckedOut = true;
                }

                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }

        private async Task<DialogTurnResult> EmailPrompt(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(sc.Context, () => new HospitalityUserSkillState(HotelService));
            if (userState.CheckedOut && string.IsNullOrWhiteSpace(userState.Email))
            {
                // prompt for email to send receipt to
                return await sc.PromptAsync(DialogIds.EmailPrompt, new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivity(CheckOutResponses.EmailPrompt),
                    RetryPrompt = TemplateManager.GenerateActivity(CheckOutResponses.InvalidEmailPrompt)
                });
            }

            return await sc.NextAsync();
        }

        private async Task<bool> ValidateEmailAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(promptContext.Context, () => new HospitalityUserSkillState(HotelService));

            // check for valid email input
            string response = promptContext.Recognized?.Value;

            if (promptContext.Recognized.Succeeded && !string.IsNullOrWhiteSpace(response) && new EmailAddressAttribute().IsValid(response))
            {
                userState.Email = response;
                return await Task.FromResult(true);
            }

            return await Task.FromResult(false);
        }

        private async Task<DialogTurnResult> EndDialog(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserStateAccessor.GetAsync(sc.Context, () => new HospitalityUserSkillState(HotelService));

            if (userState.CheckedOut)
            {
                var tokens = new Dictionary<string, object>
                {
                    { "Email", userState.Email },
                };

                // TODO process request to send email receipt
                // checked out confirmation message
                await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(CheckOutResponses.SendEmailMessage, tokens));
                await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(CheckOutResponses.CheckOutSuccess));

                return await sc.EndDialogAsync(await CreateSuccessActionResult(sc.Context));
            }

            return await sc.EndDialogAsync();
        }

        private class DialogIds
        {
            public const string CheckOutPrompt = "checkOutPrompt";
            public const string EmailPrompt = "emailPrompt";
        }
    }
}
