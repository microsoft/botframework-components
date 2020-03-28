// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
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
    public class ForwardEmailDialog : EmailSkillDialogBase
    {
        public ForwardEmailDialog(
            IServiceProvider serviceProvider)
            : base(nameof(ForwardEmailDialog), serviceProvider)
        {
            var forwardEmail = new WaterfallStep[]
            {
                IfClearContextStepAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                SetDisplayConfigAsync,
                CollectSelectedEmailAsync,
                AfterCollectSelectedEmailAsync,
                CollectRecipientAsync,
                CollectAdditionalTextAsync,
                AfterCollectAdditionalTextAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                ConfirmBeforeSendingAsync,
                ConfirmAllRecipientAsync,
                AfterConfirmPromptAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                ForwardEmailAsync,
            };

            var showEmail = new WaterfallStep[]
            {
                PagingStepAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                ShowEmailsAsync,
            };

            var collectRecipients = new WaterfallStep[]
            {
                PromptRecipientCollectionAsync,
                GetRecipientsAsync,
            };

            var updateSelectMessage = new WaterfallStep[]
            {
                UpdateMessageAsync,
                PromptUpdateMessageAsync,
                AfterUpdateMessageAsync,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.Forward, forwardEmail));
            AddDialog(new WaterfallDialog(Actions.Show, showEmail));
            AddDialog(new WaterfallDialog(Actions.CollectRecipient, collectRecipients));
            AddDialog(new WaterfallDialog(Actions.UpdateSelectMessage, updateSelectMessage));
            AddDialog(new FindContactDialog(serviceProvider));
            InitialDialogId = Actions.Forward;
        }

        public async Task<DialogTurnResult> ForwardEmailAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                sc.Context.TurnState.TryGetValue(StateProperties.APIToken, out var token);
                var message = state.Message;
                var id = message.FirstOrDefault()?.Id;
                var recipients = state.FindContactInfor.Contacts;

                var service = ServiceManager.InitMailService(token as string, state.GetUserTimeZone(), state.MailSourceType);

                // send user message.
                var content = state.Content.Equals(EmailCommonStrings.EmptyContent) ? string.Empty : state.Content;
                await service.ForwardMessageAsync(id, content, recipients);

                var emailCard = new EmailCardData
                {
                    Subject = state.Subject.Equals(EmailCommonStrings.EmptySubject) ? null : state.Subject,
                    EmailContent = state.Content.Equals(EmailCommonStrings.EmptyContent) ? null : state.Content,
                };
                emailCard = await ProcessRecipientPhotoUrlAsync(sc.Context, emailCard, state.FindContactInfor.Contacts, cancellationToken);

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