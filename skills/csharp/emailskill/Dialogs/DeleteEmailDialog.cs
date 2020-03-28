// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Extensions;
using EmailSkill.Models;
using EmailSkill.Models.Action;
using EmailSkill.Responses.DeleteEmail;
using EmailSkill.Responses.Shared;
using EmailSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Util;

namespace EmailSkill.Dialogs
{
    public class DeleteEmailDialog : EmailSkillDialogBase
    {
        public DeleteEmailDialog(
            IServiceProvider serviceProvider)
            : base(nameof(DeleteEmailDialog), serviceProvider)
        {
            var deleteEmail = new WaterfallStep[]
            {
                IfClearContextStepAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                SetDisplayConfigAsync,
                CollectSelectedEmailAsync,
                AfterCollectSelectedEmailAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                PromptToDeleteAsync,
                AfterConfirmPromptAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                DeleteEmailAsync,
            };

            var showEmail = new WaterfallStep[]
            {
                PagingStepAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                ShowEmailsAsync,
            };

            var updateSelectMessage = new WaterfallStep[]
            {
                UpdateMessageAsync,
                PromptUpdateMessageAsync,
                AfterUpdateMessageAsync,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.Delete, deleteEmail));
            AddDialog(new WaterfallDialog(Actions.Show, showEmail));
            AddDialog(new WaterfallDialog(Actions.UpdateSelectMessage, updateSelectMessage));
            InitialDialogId = Actions.Delete;
        }

        public async Task<DialogTurnResult> PromptToDeleteAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var skillOptions = (EmailSkillDialogOptions)sc.Options;

                var message = state.Message?.FirstOrDefault();
                if (message != null)
                {
                    var nameListString = DisplayHelper.ToDisplayRecipientsString_Summay(message.ToRecipients);
                    var senderIcon = await GetUserPhotoUrlAsync(sc.Context, message.Sender.EmailAddress, cancellationToken);
                    var emailCard = new EmailCardData
                    {
                        Subject = message.Subject,
                        EmailContent = message.BodyPreview,
                        Sender = message.Sender.EmailAddress.Name,
                        EmailLink = message.WebLink,
                        ReceivedDateTime = message?.ReceivedDateTime == null
                            ? CommonStrings.NotAvailable
                            : message.ReceivedDateTime.Value.UtcDateTime.ToDetailRelativeString(state.GetUserTimeZone()),
                        Speak = SpeakHelper.ToSpeechEmailDetailOverallString(message, state.GetUserTimeZone()),
                        SenderIcon = senderIcon
                    };
                    emailCard = await ProcessRecipientPhotoUrlAsync(sc.Context, emailCard, message.ToRecipients, cancellationToken);

                    var speech = SpeakHelper.ToSpeechEmailSendDetailString(message.Subject, nameListString, message.BodyPreview);
                    var prompt = TemplateManager.GenerateActivityForLocale(
                        DeleteEmailResponses.DeleteConfirm,
                        new
                        {
                            emailInfo = speech,
                            emailDetails = emailCard
                        });

                    var retry = TemplateManager.GenerateActivityForLocale(EmailSharedResponses.ConfirmSendFailed);
                    return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = prompt as Activity, RetryPrompt = retry as Activity });
                }

                skillOptions.SubFlowMode = true;
                return await sc.BeginDialogAsync(Actions.UpdateSelectMessage, skillOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> DeleteEmailAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                sc.Context.TurnState.TryGetValue(StateProperties.APIToken, out var token);
                var mailService = this.ServiceManager.InitMailService(token as string, state.GetUserTimeZone(), state.MailSourceType);
                var focusMessage = state.Message.FirstOrDefault();
                await mailService.DeleteMessageAsync(focusMessage.Id);
                var activity = TemplateManager.GenerateActivityForLocale(DeleteEmailResponses.DeleteSuccessfully);
                await sc.Context.SendActivityAsync(activity, cancellationToken);

                if (state.IsAction)
                {
                    var actionResult = new ActionResult(true);
                    return await sc.EndDialogAsync(actionResult, cancellationToken);
                }

                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}