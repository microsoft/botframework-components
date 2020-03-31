// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Models;
using EmailSkill.Models.Action;
using EmailSkill.Prompts;
using EmailSkill.Responses.SendEmail;
using EmailSkill.Responses.Shared;
using EmailSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;

namespace EmailSkill.Dialogs
{
    public class SendEmailDialog : EmailSkillDialogBase
    {
        public SendEmailDialog(
            IServiceProvider serviceProvider)
            : base(nameof(SendEmailDialog), serviceProvider)
        {
            var sendEmail = new WaterfallStep[]
            {
                IfClearContextStepAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                CollectRecipientAsync,
                CollectSubjectAsync,
                CollectTextAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                ConfirmBeforeSendingAsync,
                ConfirmAllRecipientAsync,
                AfterConfirmPromptAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                SendEmailAsync,
            };

            var collectRecipients = new WaterfallStep[]
            {
                PromptRecipientCollectionAsync,
                GetRecipientsAsync,
            };

            var updateSubject = new WaterfallStep[]
            {
                UpdateSubjectAsync,
                RetryCollectSubjectAsync,
                AfterUpdateSubjectAsync,
            };

            var updateContent = new WaterfallStep[]
            {
                UpdateContentAsync,
                PlayBackContentAsync,
                AfterCollectContentAsync,
            };

            var getRecreateInfo = new WaterfallStep[]
            {
                GetRecreateInfoAsync,
                AfterGetRecreateInfoAsync,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.Send, sendEmail));
            AddDialog(new WaterfallDialog(Actions.CollectRecipient, collectRecipients));
            AddDialog(new WaterfallDialog(Actions.UpdateSubject, updateSubject));
            AddDialog(new WaterfallDialog(Actions.UpdateContent, updateContent));
            AddDialog(new FindContactDialog(serviceProvider));
            AddDialog(new WaterfallDialog(Actions.GetRecreateInfo, getRecreateInfo));
            AddDialog(new GetRecreateInfoPrompt(Actions.GetRecreateInfoPrompt));
            InitialDialogId = Actions.Send;
        }

        public async Task<DialogTurnResult> CollectSubjectAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (state.FindContactInfor.Contacts == null || state.FindContactInfor.Contacts.Count == 0)
                {
                    state.FindContactInfor.FirstRetryInFindContact = true;
                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }

                if (!string.IsNullOrWhiteSpace(state.Subject))
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }

                bool? isSkipByDefault = false;
                isSkipByDefault = Settings.DefaultValue?.SendEmail?.First(item => item.Name == "EmailSubject")?.IsSkipByDefault;
                if (isSkipByDefault.GetValueOrDefault())
                {
                    state.Subject = string.IsNullOrEmpty(EmailCommonStrings.DefaultSubject) ? EmailCommonStrings.EmptySubject : EmailCommonStrings.DefaultSubject;

                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }

                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;
                return await sc.BeginDialogAsync(Actions.UpdateSubject, skillOptions);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> UpdateSubjectAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (state.FindContactInfor.Contacts == null || state.FindContactInfor.Contacts.Count == 0)
                {
                    state.FindContactInfor.FirstRetryInFindContact = true;
                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }

                var recipientConfirmedMessage = TemplateManager.GenerateActivityForLocale(EmailSharedResponses.RecipientConfirmed, new { userName = await GetNameListStringAsync(sc, false) });
                var noSubjectMessage = TemplateManager.GenerateActivityForLocale(SendEmailResponses.NoSubject);
                noSubjectMessage.Text = recipientConfirmedMessage.Text + " " + noSubjectMessage.Text;
                noSubjectMessage.Speak = recipientConfirmedMessage.Speak + " " + noSubjectMessage.Speak;

                return await sc.PromptAsync(Actions.Prompt, new PromptOptions() { Prompt = noSubjectMessage as Activity }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> RetryCollectSubjectAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (sc.Result != null)
                {
                    var subjectInput = sc.Context.Activity.Text;

                    if (!EmailCommonPhrase.GetIsSkip(subjectInput))
                    {
                        state.Subject = subjectInput;
                    }
                }

                if (!string.IsNullOrWhiteSpace(state.Subject))
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }

                var activity = TemplateManager.GenerateActivityForLocale(SendEmailResponses.RetryNoSubject);
                return await sc.PromptAsync(Actions.Prompt, new PromptOptions() { Prompt = activity as Activity }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterUpdateSubjectAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (!string.IsNullOrWhiteSpace(state.Subject))
                {
                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }

                if (sc.Result != null)
                {
                    var subjectInput = sc.Context.Activity.Text;

                    if (!EmailCommonPhrase.GetIsSkip(subjectInput))
                    {
                        state.Subject = subjectInput;
                    }
                    else
                    {
                        state.Subject = EmailCommonStrings.EmptySubject;
                    }
                }

                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> CollectTextAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (state.FindContactInfor.Contacts == null || state.FindContactInfor.Contacts.Count == 0)
                {
                    state.FindContactInfor.FirstRetryInFindContact = true;
                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }

                if (!string.IsNullOrWhiteSpace(state.Content))
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }

                bool? isSkipByDefault = false;
                isSkipByDefault = Settings.DefaultValue?.SendEmail?.First(item => item.Name == "EmailMessage")?.IsSkipByDefault;
                if (isSkipByDefault.GetValueOrDefault())
                {
                    state.Subject = string.IsNullOrEmpty(EmailCommonStrings.DefaultContent) ? EmailCommonStrings.EmptyContent : EmailCommonStrings.DefaultContent;

                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }

                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;
                return await sc.BeginDialogAsync(Actions.UpdateContent, skillOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> UpdateContentAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var activity = TemplateManager.GenerateActivityForLocale(SendEmailResponses.NoMessageBody);
                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = activity as Activity }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> PlayBackContentAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (sc.Result != null)
                {
                    var contentInput = sc.Context.Activity.Text;

                    if (!EmailCommonPhrase.GetIsSkip(contentInput))
                    {
                        state.Content = contentInput;

                        var replyMessage = TemplateManager.GenerateActivityForLocale(
                        SendEmailResponses.PlayBackMessage,
                        new
                        {
                            emailContent = state.Content,
                        });

                        var confirmMessageRetryActivity = TemplateManager.GenerateActivityForLocale(SendEmailResponses.ConfirmMessageRetry);
                        return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions()
                        {
                            Prompt = replyMessage as Activity,
                            RetryPrompt = confirmMessageRetryActivity as Activity,
                        }, cancellationToken);
                    }
                    else
                    {
                        state.Content = EmailCommonStrings.EmptyContent;
                        return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                    }
                }

                return await sc.NextAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterCollectContentAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    return await sc.EndDialogAsync(true, cancellationToken);
                }

                var activity = TemplateManager.GenerateActivityForLocale(SendEmailResponses.RetryContent);
                await sc.Context.SendActivityAsync(activity, cancellationToken);
                return await sc.ReplaceDialogAsync(Actions.GetRecreateInfo, options: sc.Options, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> SendEmailAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                sc.Context.TurnState.TryGetValue(StateProperties.APIToken, out var token);

                var service = ServiceManager.InitMailService(token as string, state.GetUserTimeZone(), state.MailSourceType);

                // send user message.
                var subject = state.Subject.Equals(EmailCommonStrings.EmptySubject) ? string.Empty : state.Subject;
                var content = state.Content.Equals(EmailCommonStrings.EmptyContent) ? string.Empty : state.Content;
                await service.SendMessageAsync(content, subject, state.FindContactInfor.Contacts);

                var emailCard = new EmailCardData
                {
                    Subject = state.Subject.Equals(EmailCommonStrings.EmptySubject) ? null : string.Format(EmailCommonStrings.SubjectFormat, state.Subject),
                    EmailContent = state.Content.Equals(EmailCommonStrings.EmptyContent) ? null : string.Format(EmailCommonStrings.ContentFormat, state.Content),
                };
                emailCard = await ProcessRecipientPhotoUrlAsync(sc.Context, emailCard, state.FindContactInfor.Contacts, cancellationToken);

                var replyMessage = TemplateManager.GenerateActivityForLocale(
                    EmailSharedResponses.SentSuccessfully,
                    new
                    {
                        subject = state.Subject,
                        emailDetails = emailCard
                    });

                await sc.Context.SendActivityAsync(replyMessage, cancellationToken);
                if (state.IsAction)
                {
                    var actionResult = new ActionResult(true);
                    await ClearConversationStateAsync(sc, cancellationToken);
                    return await sc.EndDialogAsync(actionResult, cancellationToken);
                }

                await ClearConversationStateAsync(sc, cancellationToken);
                return await sc.EndDialogAsync(true, cancellationToken);
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> GetRecreateInfoAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var getRecreateInfoActivity = TemplateManager.GenerateActivityForLocale(SendEmailResponses.GetRecreateInfo);
                var getRecreateInfoRetryActivity = TemplateManager.GenerateActivityForLocale(SendEmailResponses.GetRecreateInfoRetry);
                return await sc.PromptAsync(Actions.GetRecreateInfoPrompt, new PromptOptions
                {
                    Prompt = getRecreateInfoActivity as Activity,
                    RetryPrompt = getRecreateInfoRetryActivity as Activity
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        public async Task<DialogTurnResult> AfterGetRecreateInfoAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;
                if (sc.Result != null)
                {
                    var recreateState = sc.Result as ResendEmailState?;
                    switch (recreateState.Value)
                    {
                        case ResendEmailState.Cancel:
                            var activity = TemplateManager.GenerateActivityForLocale(EmailSharedResponses.CancellingMessage);
                            await sc.Context.SendActivityAsync(activity, cancellationToken);
                            await ClearConversationStateAsync(sc, cancellationToken);
                            return await sc.EndDialogAsync(false, cancellationToken);
                        case ResendEmailState.Recipients:
                            state.ClearParticipants();
                            return await sc.ReplaceDialogAsync(Actions.Send, options: skillOptions, cancellationToken: cancellationToken);
                        case ResendEmailState.Subject:
                            state.ClearSubject();
                            return await sc.ReplaceDialogAsync(Actions.Send, options: skillOptions, cancellationToken: cancellationToken);
                        case ResendEmailState.Body:
                            state.ClearContent();
                            return await sc.ReplaceDialogAsync(Actions.Send, options: skillOptions, cancellationToken: cancellationToken);
                        default:
                            // should not go to this part. place an error handling for save.
                            await HandleDialogExceptionsAsync(sc, new Exception("Get unexpect state in recreate."), cancellationToken);
                            return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
                    }
                }
                else
                {
                    // should not go to this part. place an error handling for save.
                    await HandleDialogExceptionsAsync(sc, new Exception("Get unexpect result in recreate."), cancellationToken);
                    return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}