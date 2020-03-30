// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Models;
using EmailSkill.Models.Action;
using EmailSkill.Responses.Shared;
using EmailSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Util;

namespace EmailSkill.Dialogs
{
    public class ReplyEmailDialog : EmailSkillDialogBase
    {
        public ReplyEmailDialog(
            IServiceProvider serviceProvider)
            : base(nameof(ReplyEmailDialog), serviceProvider)
        {
            var replyEmail = new WaterfallStep[]
            {
                IfClearContextStepAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                SetDisplayConfigAsync,
                CollectSelectedEmailAsync,
                AfterCollectSelectedEmailAsync,
                CollectAdditionalTextAsync,
                AfterCollectAdditionalTextAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                ConfirmBeforeSendingAsync,
                AfterConfirmPromptAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                ReplyEmailAsync,
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
            AddDialog(new WaterfallDialog(Actions.Reply, replyEmail));

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.Show, showEmail));
            AddDialog(new WaterfallDialog(Actions.UpdateSelectMessage, updateSelectMessage));

            InitialDialogId = Actions.Reply;
        }

        public async Task<DialogTurnResult> ReplyEmailAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                sc.Context.TurnState.TryGetValue(StateProperties.APIToken, out var token);
                var message = state.Message.FirstOrDefault();

                var service = ServiceManager.InitMailService(token as string, state.GetUserTimeZone(), state.MailSourceType);

                // reply user message.
                if (message != null)
                {
                    var content = state.Content.Equals(EmailCommonStrings.EmptyContent) ? string.Empty : state.Content;
                    await service.ReplyToMessageAsync(message.Id, content);
                }

                var emailCard = new EmailCardData
                {
                    Subject = state.Subject.Equals(EmailCommonStrings.EmptySubject) ? null : state.Subject,
                    EmailContent = state.Content.Equals(EmailCommonStrings.EmptyContent) ? null : state.Content,
                };
                emailCard = await ProcessRecipientPhotoUrlAsync(sc.Context, emailCard, state.FindContactInfor.Contacts, cancellationToken);

                var stringToken = new StringDictionary
                {
                    { "Subject", state.Subject },
                };

                var reply = TemplateManager.GenerateActivityForLocale(
                EmailSharedResponses.SentSuccessfully,
                new
                {
                    subject = state.Subject,
                    emailDetails = emailCard
                });

                await sc.Context.SendActivityAsync(reply);

                if (state.IsAction)
                {
                    var actionResult = new ActionResult(true);
                    await ClearConversationStateAsync(sc, cancellationToken);
                    return await sc.EndDialogAsync(actionResult, cancellationToken);
                }

                await ClearConversationStateAsync(sc, cancellationToken);
                return await sc.EndDialogAsync(true, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}