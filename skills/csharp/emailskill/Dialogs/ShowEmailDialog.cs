// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Extensions;
using EmailSkill.Models;
using EmailSkill.Models.Action;
using EmailSkill.Responses.Shared;
using EmailSkill.Responses.ShowEmail;
using EmailSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;

namespace EmailSkill.Dialogs
{
    public class ShowEmailDialog : EmailSkillDialogBase
    {
        public ShowEmailDialog(
            IServiceProvider serviceProvider)
            : base(nameof(ShowEmailDialog), serviceProvider)
        {
            var showEmail = new WaterfallStep[]
            {
                IfClearContextStepAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                DisplayAsync
            };

            var readEmail = new WaterfallStep[]
            {
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                ReadEmailAsync,
                ReshowAsync
            };

            var deleteEmail = new WaterfallStep[]
            {
                DeleteEmailAsync,
                ReshowAsync
            };

            var forwardEmail = new WaterfallStep[]
            {
                ForwardEmailAsync,
                ReshowAsync
            };

            var replyEmail = new WaterfallStep[]
            {
                ReplyEmailAsync,
                ReshowAsync
            };

            var displayEmail = new WaterfallStep[]
            {
                IfClearPagingConditionStepAsync,
                PagingStepAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                ShowEmailsAsync,
                PromptToHandleAsync,
                CheckReadAsync,
                HandleMoreAsync
            };

            var displayFilteredEmail = new WaterfallStep[]
            {
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                ShowFilteredEmailsAsync,
                PromptToHandleAsync,
                CheckReadAsync,
                HandleMoreAsync
            };

            var redisplayEmail = new WaterfallStep[]
            {
                PromptToReshowAsync,
                CheckReshowAsync,
                HandleMoreAsync,
            };

            var selectEmail = new WaterfallStep[]
            {
                SelectEmailPromptAsync,
                HandleMoreAsync
            };

            var forwardEmailDialog = serviceProvider.GetService<ForwardEmailDialog>();
            var replyEmailDialog = serviceProvider.GetService<ReplyEmailDialog>();
            var deleteEmailDialog = serviceProvider.GetService<DeleteEmailDialog>();

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.Show, showEmail));
            AddDialog(new WaterfallDialog(Actions.Read, readEmail));
            AddDialog(new WaterfallDialog(Actions.Delete, deleteEmail));
            AddDialog(new WaterfallDialog(Actions.Forward, forwardEmail));
            AddDialog(new WaterfallDialog(Actions.Reply, replyEmail));
            AddDialog(new WaterfallDialog(Actions.Display, displayEmail));
            AddDialog(new WaterfallDialog(Actions.DisplayFiltered, displayFilteredEmail));
            AddDialog(new WaterfallDialog(Actions.ReDisplay, redisplayEmail));
            AddDialog(new WaterfallDialog(Actions.SelectEmail, selectEmail));
            AddDialog(deleteEmailDialog ?? throw new ArgumentNullException(nameof(deleteEmailDialog)));
            AddDialog(replyEmailDialog ?? throw new ArgumentNullException(nameof(replyEmailDialog)));
            AddDialog(forwardEmailDialog ?? throw new ArgumentNullException(nameof(forwardEmailDialog)));
            InitialDialogId = Actions.Show;
        }

        protected async Task<DialogTurnResult> IfClearPagingConditionStepAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                // Clear focus item
                state.UserSelectIndex = -1;

                // Clear search condition
                state.SenderName = null;
                state.SearchTexts = null;

                return await sc.NextAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> PromptToHandleAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var activity = TemplateManager.GenerateActivityForLocale(ShowEmailResponses.ReadOut, new { messageList = state.MessageList });
                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = activity as Activity }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> SelectEmailPromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var activity = TemplateManager.GenerateActivityForLocale(ShowEmailResponses.ActionPrompt);
                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = activity as Activity }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> PromptToReshowAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var activity = TemplateManager.GenerateActivityForLocale(ShowEmailResponses.ReadOutMore);
                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = activity as Activity }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> CheckReadAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;

                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var luisResult = sc.Context.TurnState.Get<EmailLuis>(StateProperties.EmailLuisResult);

                var topIntent = luisResult?.TopIntent().intent;
                if (topIntent == null)
                {
                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }

                var userInput = sc.Context.Activity.Text;
                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);
                if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == false)
                {
                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }
                else if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true)
                {
                    return await sc.ReplaceDialogAsync(Actions.Read, skillOptions, cancellationToken);
                }

                return await sc.NextAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> ReadEmailAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var skillOptions = (EmailSkillDialogOptions)sc.Options;

                var userInput = sc.Context.Activity.Text;

                var luisResult = sc.Context.TurnState.Get<EmailLuis>(StateProperties.EmailLuisResult);
                var topIntent = luisResult?.TopIntent().intent;
                var generalLuisResult = sc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResult);
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                if (topIntent == null)
                {
                    return await sc.EndDialogAsync(true, cancellationToken);
                }

                await DigestFocusEmailAsync(sc, cancellationToken);

                var message = state.Message.FirstOrDefault();
                if (message == null)
                {
                    state.Message.Add(state.MessageList[0]);
                    message = state.Message.FirstOrDefault();
                }

                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);

                if ((topIntent == EmailLuis.Intent.None
                    || topIntent == EmailLuis.Intent.SearchMessages
                    || (topIntent == EmailLuis.Intent.ReadAloud && !IsReadMoreIntent(generalTopIntent, sc.Context.Activity.Text))
                    || (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true))
                    && message != null)
                {
                    var senderIcon = await GetUserPhotoUrlAsync(sc.Context, message.Sender.EmailAddress, cancellationToken);
                    var emailCard = new EmailCardData
                    {
                        Subject = message.Subject,
                        Sender = message.Sender.EmailAddress.Name,
                        EmailContent = message.BodyPreview,
                        EmailLink = message.WebLink,
                        ReceivedDateTime = message?.ReceivedDateTime == null
                            ? CommonStrings.NotAvailable
                            : message.ReceivedDateTime.Value.UtcDateTime.ToDetailRelativeString(state.GetUserTimeZone()),
                        Speak = SpeakHelper.ToSpeechEmailDetailOverallString(message, state.GetUserTimeZone()),
                        SenderIcon = senderIcon
                    };

                    emailCard = await ProcessRecipientPhotoUrlAsync(sc.Context, emailCard, message.ToRecipients, cancellationToken);
                    var replyMessage = TemplateManager.GenerateActivityForLocale(
                        ShowEmailResponses.ReadOutMessage,
                        new
                        {
                            emailDetailsWithoutContent = SpeakHelper.ToSpeechEmailDetailString(message, state.GetUserTimeZone()),
                            emailDetailsWithContent = SpeakHelper.ToSpeechEmailDetailString(message, state.GetUserTimeZone(), true),
                            emailDetails = emailCard
                        });

                    // Set email as read.
                    sc.Context.TurnState.TryGetValue(StateProperties.APIToken, out var token);
                    var service = ServiceManager.InitMailService(token as string, state.GetUserTimeZone(), state.MailSourceType);
                    await service.MarkMessageAsReadAsync(message.Id);

                    await sc.Context.SendActivityAsync(replyMessage, cancellationToken);
                }

                return await sc.NextAsync(cancellationToken: cancellationToken);
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

        protected async Task<DialogTurnResult> CheckReshowAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;

                var userInput = sc.Context.Activity.Text;
                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);
                if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == false)
                {
                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }
                else if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true)
                {
                    return await sc.ReplaceDialogAsync(Actions.Display, skillOptions, cancellationToken);
                }

                return await sc.NextAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> HandleMoreAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var luisResult = sc.Context.TurnState.Get<EmailLuis>(StateProperties.EmailLuisResult);
                var topIntent = luisResult?.TopIntent().intent;

                var generalIntent = sc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResult);
                var topGeneralIntent = generalIntent?.TopIntent().intent;
                if (topIntent == null)
                {
                    return await sc.EndDialogAsync(true, cancellationToken);
                }

                var userInput = sc.Context.Activity.Text;

                await DigestFocusEmailAsync(sc, cancellationToken);

                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;

                if (topIntent == EmailLuis.Intent.Delete ||
                    topIntent == EmailLuis.Intent.Forward ||
                    topIntent == EmailLuis.Intent.Reply)
                {
                    if (state.Message.Count == 0 && state.MessageList.Count > 1)
                    {
                        return await sc.ReplaceDialogAsync(Actions.SelectEmail, skillOptions, cancellationToken);
                    }
                    else
                    {
                        if (state.Message.Count == 0)
                        {
                            state.Message.Add(state.MessageList[0]);
                        }

                        if (topIntent == EmailLuis.Intent.Delete)
                        {
                            return await sc.BeginDialogAsync(Actions.Delete, skillOptions, cancellationToken);
                        }
                        else if (topIntent == EmailLuis.Intent.Forward)
                        {
                            return await sc.BeginDialogAsync(Actions.Forward, skillOptions, cancellationToken);
                        }
                        else if (topIntent == EmailLuis.Intent.Reply)
                        {
                            return await sc.BeginDialogAsync(Actions.Reply, skillOptions, cancellationToken);
                        }
                    }
                }

                if (IsReadMoreIntent(topGeneralIntent, userInput)
                    || (topIntent == EmailLuis.Intent.ShowNext || topIntent == EmailLuis.Intent.ShowPrevious || topGeneralIntent == General.Intent.ShowPrevious || topGeneralIntent == General.Intent.ShowNext))
                {
                    return await sc.ReplaceDialogAsync(Actions.Display, skillOptions, cancellationToken);
                }
                else
                {
                    await DigestEmailLuisResultAsync(sc, true, cancellationToken);
                    await SearchEmailsFromListAsync(sc, cancellationToken);

                    if (state.MessageList.Count > 0)
                    {
                        return await sc.ReplaceDialogAsync(Actions.DisplayFiltered, skillOptions, cancellationToken);
                    }

                    var activity = TemplateManager.GenerateActivityForLocale(EmailSharedResponses.DidntUnderstandMessage);
                    await sc.Context.SendActivityAsync(activity, cancellationToken);
                    return await sc.EndDialogAsync(true, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> DeleteEmailAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;

                return await sc.BeginDialogAsync(nameof(DeleteEmailDialog), skillOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> ForwardEmailAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;

                return await sc.BeginDialogAsync(nameof(ForwardEmailDialog), skillOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> ReplyEmailAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;

                return await sc.BeginDialogAsync(nameof(ReplyEmailDialog), skillOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> DisplayAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var options = sc.Options as EmailSkillDialogOptions;
                if (state.IsAction)
                {
                    sc.Context.TurnState.TryGetValue(StateProperties.APIToken, out var token);
                    var serivce = ServiceManager.InitMailService(token as string, state.GetUserTimeZone(), state.MailSourceType);

                    var isUnreadOnly = state.IsUnreadOnly;
                    var isImportant = state.IsImportant;
                    var startDateTime = state.StartDateTime;
                    var endDateTime = state.EndDateTime;
                    var directlyToMe = state.DirectlyToMe;
                    string mailAddress = null;

                    // Get user message.
                    var emailResult = await serivce.GetMyMessagesAsync(startDateTime, endDateTime, isUnreadOnly, isImportant, directlyToMe, mailAddress);
                    var actionResult = new SummaryResult();
                    actionResult.EmailList = new List<EmailInfo>();
                    foreach (var email in emailResult)
                    {
                        var emailInfo = new EmailInfo()
                        {
                            Subject = email.Subject,
                            Content = email.Body.Content,
                            Sender = email.Sender.EmailAddress.Address,
                            Reciever = email.ToRecipients.Select(li => li.EmailAddress.Address).ToList()
                        };

                        actionResult.EmailList.Add(emailInfo);
                    }

                    actionResult.ActionSuccess = true;

                    return await sc.EndDialogAsync(actionResult, cancellationToken);
                }

                return await sc.ReplaceDialogAsync(Actions.Display, options, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> ReshowAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                return await sc.ReplaceDialogAsync(Actions.ReDisplay, skillOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> ShowFilteredEmailsAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (state.MessageList.Count > 0)
                {
                    if (state.Message.Count == 0)
                    {
                        state.Message.Add(state.MessageList[0]);

                        if (state.MessageList.Count > 1)
                        {
                            var importCount = 0;

                            foreach (var msg in state.MessageList)
                            {
                                if (msg.Importance.HasValue && msg.Importance.Value == Importance.High)
                                {
                                    importCount++;
                                }
                            }

                            await ShowMailListAsync(sc, state.MessageList, state.MessageList.Count(), importCount, cancellationToken);
                            return await sc.NextAsync(cancellationToken: cancellationToken);
                        }
                        else if (state.MessageList.Count == 1)
                        {
                            return await sc.ReplaceDialogAsync(Actions.Read, options: sc.Options, cancellationToken);
                        }
                    }
                    else
                    {
                        return await sc.ReplaceDialogAsync(Actions.Read, options: sc.Options, cancellationToken);
                    }

                    var activity = TemplateManager.GenerateActivityForLocale(EmailSharedResponses.DidntUnderstandMessage);
                    await sc.Context.SendActivityAsync(activity, cancellationToken);
                    return await sc.EndDialogAsync(true, cancellationToken);
                }
                else
                {
                    var activity = TemplateManager.GenerateActivityForLocale(EmailSharedResponses.EmailNotFound);
                    await sc.Context.SendActivityAsync(activity, cancellationToken);
                }

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
    }
}