﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Extensions;
using EmailSkill.Models;
using EmailSkill.Models.Action;
using EmailSkill.Responses.Shared;
using EmailSkill.Services;
using EmailSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using Microsoft.Recognizers.Text;

namespace EmailSkill.Dialogs
{
    public class EmailSkillDialogBase : ComponentDialog
    {
        public EmailSkillDialogBase(
             string dialogId,
             IServiceProvider serviceProvider)
             : base(dialogId)
        {
            TemplateManager = serviceProvider.GetService<LocaleTemplateManager>();

            Settings = serviceProvider.GetService<BotSettings>();
            Services = serviceProvider.GetService<BotServices>();

            var conversationState = serviceProvider.GetService<ConversationState>();
            EmailStateAccessor = conversationState.CreateProperty<EmailSkillState>(nameof(EmailSkillState));
            DialogStateAccessor = conversationState.CreateProperty<DialogState>(nameof(DialogState));

            ServiceManager = serviceProvider.GetService<IServiceManager>();

            var appCredentials = serviceProvider.GetService<MicrosoftAppCredentials>();
            AddDialog(new MultiProviderAuthDialog(Settings.OAuthConnections));
            AddDialog(new TextPrompt(Actions.Prompt));
            AddDialog(new ConfirmPrompt(Actions.TakeFurtherAction, null, Culture.English) { Style = ListStyle.SuggestedAction });
        }

        protected EmailSkillDialogBase(string dialogId)
            : base(dialogId)
        {
        }

        protected LocaleTemplateManager TemplateManager { get; set; }

        protected BotServices Services { get; set; }

        protected BotSettings Settings { get; set; }

        protected IStatePropertyAccessor<EmailSkillState> EmailStateAccessor { get; set; }

        protected IStatePropertyAccessor<DialogState> DialogStateAccessor { get; set; }

        protected IServiceManager ServiceManager { get; set; }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            await DigestEmailLuisResultAsync(dc, true, cancellationToken);
            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            await DigestEmailLuisResultAsync(dc, false, cancellationToken);
            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }

        protected override Task<DialogTurnResult> EndComponentAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            var resultString = result?.ToString();
            if (!string.IsNullOrWhiteSpace(resultString) && resultString.Equals(CommonUtil.DialogTurnResultCancelAllDialogs, StringComparison.InvariantCultureIgnoreCase))
            {
                return outerDc.CancelAllDialogsAsync(cancellationToken);
            }
            else
            {
                return base.EndComponentAsync(outerDc, result, cancellationToken);
            }
        }

        // Shared steps
        protected virtual async Task<DialogTurnResult> IfClearContextStepAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                var luisResult = sc.Context.TurnState.Get<EmailLuis>(StateProperties.EmailLuisResult);
                var skillLuisResult = luisResult?.TopIntent().intent;
                var generalLuisResult = sc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResult);
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                if (skillOptions == null || (!skillOptions.SubFlowMode && !state.IsAction))
                {
                    // Clear email state data
                    await ClearConversationStateAsync(sc, cancellationToken);
                    await DigestEmailLuisResultAsync(sc, true, cancellationToken);

                    state.GeneralSearchTexts = state.SearchTexts;
                    state.GeneralSenderName = state.SenderName;
                }

                return await sc.NextAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected virtual async Task<DialogTurnResult> SetDisplayConfigAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (skillOptions == null || !skillOptions.SubFlowMode)
                {
                    // For forward/reply/display email, display all emails by default.
                    state.IsUnreadOnly = false;
                }

                return await sc.NextAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected virtual async Task<DialogTurnResult> PagingStepAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                var luisResult = sc.Context.TurnState.Get<EmailLuis>(StateProperties.EmailLuisResult);
                var skillLuisResult = luisResult?.TopIntent().intent;
                var generalLuisResult = sc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResult);
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                if (skillLuisResult == EmailLuis.Intent.ShowNext || generalTopIntent == General.Intent.ShowNext)
                {
                    state.ShowEmailIndex++;
                }
                else if ((skillLuisResult == EmailLuis.Intent.ShowPrevious || generalTopIntent == General.Intent.ShowPrevious) && state.ShowEmailIndex >= 0)
                {
                    state.ShowEmailIndex--;
                }
                else if (IsReadMoreIntent(generalTopIntent, sc.Context.Activity.Text))
                {
                    state.ShowEmailIndex++;
                }

                await DigestFocusEmailAsync(sc, cancellationToken);

                return await sc.NextAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // Shared steps
        protected async Task<DialogTurnResult> GetAuthTokenAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var activity = TemplateManager.GenerateActivityForLocale(EmailSharedResponses.NoAuth);
                return await sc.PromptAsync(nameof(MultiProviderAuthDialog), new PromptOptions() { RetryPrompt = activity as Activity }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterGetAuthTokenAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (sc.Result is ProviderTokenResponse providerTokenResponse)
                {
                    var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                    if (sc.Context.TurnState.TryGetValue(StateProperties.APIToken, out var token))
                    {
                        sc.Context.TurnState[StateProperties.APIToken] = providerTokenResponse.TokenResponse.Token;
                    }
                    else
                    {
                        sc.Context.TurnState.Add(StateProperties.APIToken, providerTokenResponse.TokenResponse.Token);
                    }

                    var provider = providerTokenResponse.AuthenticationProvider;

                    if (provider == OAuthProvider.AzureAD)
                    {
                        state.MailSourceType = MailSource.Microsoft;
                    }
                    else if (provider == OAuthProvider.Google)
                    {
                        state.MailSourceType = MailSource.Google;
                    }
                    else
                    {
                        throw new Exception($"The authentication provider \"{provider.ToString()}\" is not support by the Email Skill.");
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

        protected async Task<DialogTurnResult> UpdateMessageAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                return await sc.BeginDialogAsync(Actions.Show, skillOptions, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> PromptUpdateMessageAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                // End when there is no message.
                if (state.MessageList.Count == 0)
                {
                    return await sc.EndDialogAsync(true, cancellationToken);
                }

                // Set focus when there is only one message.
                if (state.MessageList.Count == 1)
                {
                    state.Message.Add(state.MessageList[0]);

                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }

                var activity = TemplateManager.GenerateActivityForLocale(EmailSharedResponses.NoFocusMessage);
                return await sc.PromptAsync(
                    Actions.Prompt,
                    new PromptOptions() { Prompt = activity as Activity });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterUpdateMessageAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var luisResult = sc.Context.TurnState.Get<EmailLuis>(StateProperties.EmailLuisResult);

                await DigestFocusEmailAsync(sc, cancellationToken);
                var focusedMessage = state.Message.FirstOrDefault();

                // todo: should set updatename reason in stepContext.Result
                if (focusedMessage == null)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateSelectMessage, skillOptions, cancellationToken);
                }
                else
                {
                    return await sc.EndDialogAsync(true, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> CollectSelectedEmailAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                skillOptions.SubFlowMode = true;

                if (state.Message == null || state.Message.Count() == 0)
                {
                    return await sc.BeginDialogAsync(Actions.UpdateSelectMessage, skillOptions, cancellationToken);
                }

                return await sc.NextAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterCollectSelectedEmailAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                // End the dialog when there is no focused email
                if (state.Message.Count == 0)
                {
                    return await sc.EndDialogAsync(true, cancellationToken);
                }

                return await sc.NextAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> ConfirmBeforeSendingAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                if (sc.Result != null && sc.Result is bool)
                {
                    var checkLastStepSuccess = (bool)sc.Result;
                    if (!checkLastStepSuccess)
                    {
                        return await sc.EndDialogAsync(true, cancellationToken);
                    }
                }

                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                string nameListString;

                var action = Actions.Send;
                if (state.FindContactInfor.Contacts.FirstOrDefault() == null)
                {
                    // this means reply confirm
                    action = Actions.Reply;
                    await GetPreviewSubjectAsync(sc, action, cancellationToken);
                }
                else if (state.Subject == null)
                {
                    // this mean forward confirm
                    action = Actions.Forward;
                    await GetPreviewSubjectAsync(sc, action, cancellationToken);
                }
                else
                {
                    action = Actions.Send;
                }

                nameListString = DisplayHelper.ToDisplayRecipientsString_Summay(state.FindContactInfor.Contacts);

                var emailCard = new EmailCardData
                {
                    Subject = state.Subject.Equals(EmailCommonStrings.EmptySubject) ? null : state.Subject,
                    EmailContent = state.Content.Equals(EmailCommonStrings.EmptyContent) ? null : state.Content,
                };
                emailCard = await ProcessRecipientPhotoUrlAsync(sc.Context, emailCard, state.FindContactInfor.Contacts, cancellationToken);

                var speech = SpeakHelper.ToSpeechEmailSendDetailString(state.Subject, nameListString, state.Content);

                if (state.FindContactInfor.Contacts.Count > DisplayHelper.MaxReadoutNumber && (action == Actions.Send || action == Actions.Forward))
                {
                    var prompt = TemplateManager.GenerateActivityForLocale(
                        EmailSharedResponses.ConfirmSendWithRecipients,
                        new
                        {
                            emailSubject = state.Subject,
                            emailDetails = emailCard
                        });

                    var retry = TemplateManager.GenerateActivityForLocale(EmailSharedResponses.ConfirmSendRecipientsFailed);

                    return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = prompt as Activity, RetryPrompt = retry as Activity }, cancellationToken);
                }
                else
                {
                    var prompt = TemplateManager.GenerateActivityForLocale(
                        EmailSharedResponses.ConfirmSendWithoutRecipients,
                        new
                        {
                            emailSubject = state.Subject,
                            emailDetails = emailCard
                        });

                    var retry = TemplateManager.GenerateActivityForLocale(EmailSharedResponses.ConfirmSendFailed);

                    return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = prompt as Activity, RetryPrompt = retry as Activity }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterConfirmPromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
                else
                {
                    var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                    if (state.IsAction)
                    {
                        var actionResult = new ActionResult(false);
                        return await sc.EndDialogAsync(actionResult, cancellationToken);
                    }

                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> ConfirmAllRecipientAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (state.FindContactInfor.Contacts.Count > DisplayHelper.MaxReadoutNumber)
                {
                    var nameListString = DisplayHelper.ToDisplayRecipientsString(state.FindContactInfor.Contacts);
                    var confirmRecipients = TemplateManager.GenerateActivityForLocale(EmailSharedResponses.ConfirmSendAfterConfirmRecipients, new { recipientsList = nameListString });
                    var retry = TemplateManager.GenerateActivityForLocale(EmailSharedResponses.ConfirmSendFailed);

                    return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = confirmRecipients as Activity, RetryPrompt = retry as Activity });
                }
                else
                {
                    return await sc.NextAsync(sc.Result, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> CollectAdditionalTextAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (string.IsNullOrEmpty(state.Content))
                {
                    var noEmailContentMessage = TemplateManager.GenerateActivityForLocale(EmailSharedResponses.NoEmailContent);
                    if (sc.ActiveDialog.Id == Actions.Forward)
                    {
                        if (state.FindContactInfor.Contacts.Count == 0 || state.FindContactInfor.Contacts == null)
                        {
                            state.FindContactInfor.FirstRetryInFindContact = true;
                            return await sc.EndDialogAsync();
                        }

                        noEmailContentMessage = TemplateManager.GenerateActivityForLocale(EmailSharedResponses.NoEmailContentWithRecipientConfirmed, new { userName = await GetNameListStringAsync(sc, false) });
                    }
                    else if (sc.ActiveDialog.Id == Actions.Reply)
                    {
                        noEmailContentMessage = TemplateManager.GenerateActivityForLocale(EmailSharedResponses.NoEmailContentForReply);
                    }

                    return await sc.PromptAsync(
                        Actions.Prompt,
                        new PromptOptions { Prompt = noEmailContentMessage as Activity },
                        cancellationToken);
                }
                else
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> CollectRecipientAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (!state.FindContactInfor.Contacts.Any())
                {
                    return await sc.BeginDialogAsync(Actions.CollectRecipient, skillOptions, cancellationToken);
                }

                return await sc.NextAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> PromptRecipientCollectionAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.FindContactInfor.ContactsNameList.Any())
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
                else
                {
                    var activity = TemplateManager.GenerateActivityForLocale(EmailSharedResponses.NoRecipients);
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = activity as Activity }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> GetRecipientsAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                // ensure state.nameList is not null or empty
                if (!state.FindContactInfor.ContactsNameList.Any())
                {
                    var userInput = sc.Result.ToString();
                    if (string.IsNullOrWhiteSpace(userInput))
                    {
                        return await sc.BeginDialogAsync(Actions.CollectRecipient, skillOptions, cancellationToken);
                    }

                    var nameList = userInput.Split(EmailCommonPhrase.GetContactNameSeparator(), options: StringSplitOptions.None)
                        .Select(x => x.Trim())
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();
                    state.FindContactInfor.ContactsNameList = nameList;
                }

                return await sc.BeginDialogAsync(nameof(FindContactDialog), options: new FindContactDialogOptions(sc.Options), cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> AfterCollectAdditionalTextAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
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
                    }
                    else
                    {
                        state.Content = EmailCommonStrings.EmptyContent;
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

        protected async Task<DialogTurnResult> ShowEmailsAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                var (messages, totalCount, importantCount) = await GetMessagesAsync(sc, cancellationToken);

                // Get display messages
                var displayMessages = new List<Message>();
                var startIndex = 0;
                for (var i = startIndex; i < messages.Count(); i++)
                {
                    displayMessages.Add(messages[i]);
                }

                if (displayMessages.Count > 0)
                {
                    state.MessageList = displayMessages;
                    state.Message.Clear();

                    await ShowMailListAsync(sc, displayMessages, totalCount, importantCount, cancellationToken);
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
                else
                {
                    state.MessageList.Clear();
                    state.Message.Clear();
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

        protected async Task<DialogTurnResult> SearchEmailsFromListAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var userInput = sc.Context.Activity.Text;

                var messages = state.MessageList;

                var luisResult = sc.Context.TurnState.Get<EmailLuis>(StateProperties.EmailLuisResult);
                var generalLuisResult = sc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResult);
                if (((luisResult.Entities.ordinal != null) && (luisResult.Entities.ordinal.Count() > 0))
                    || ((generalLuisResult?.Entities?.number != null) && (generalLuisResult.Entities.number.Count() > 0)))
                {
                    // Search by ordinal and number
                    if (state.MessageList.Count > state.UserSelectIndex)
                    {
                        state.Message.Clear();
                        state.Message.Add(state.MessageList[state.UserSelectIndex]);
                    }
                }
                else
                {
                    // Search by condition
                    var searchSender = state.SenderName?.ToLowerInvariant();
                    var searchSubject = state.SearchTexts?.ToLowerInvariant();
                    var searchUserInput = userInput?.ToLowerInvariant();

                    var searchType = EmailSearchType.None;
                    (messages, searchType) = FilterMessages(messages, searchSender, searchSubject, searchUserInput);

                    if (searchType == EmailSearchType.SearchByContact)
                    {
                        state.SearchTexts = null;
                        if (state.SenderName == null)
                        {
                            state.SenderName = userInput;
                        }
                    }
                    else if (searchType == EmailSearchType.SearchBySubject)
                    {
                        state.SenderName = null;
                        if (state.SearchTexts == null)
                        {
                            state.SearchTexts = userInput;
                        }
                    }

                    state.MessageList = messages;
                    state.Message.Clear();
                }

                return await sc.NextAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        // Validators
        protected Task<bool> TokenResponseValidator(PromptValidatorContext<Activity> pc, CancellationToken cancellationToken)
        {
            var activity = pc.Recognized.Value;
            if (activity != null && activity.Type == ActivityTypes.Event)
            {
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        protected Task<bool> AuthPromptValidator(PromptValidatorContext<TokenResponse> promptContext, CancellationToken cancellationToken)
        {
            var token = promptContext.Recognized.Value;
            if (token != null)
            {
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        // Helpers
        protected async Task<string> GetNameListStringAsync(WaterfallStepContext sc, bool isWithEmailDetail = true, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await EmailStateAccessor.GetAsync(sc?.Context, cancellationToken: cancellationToken);
            var recipients = state.FindContactInfor.Contacts;

            if (recipients == null || recipients.Count == 0)
            {
                throw new NoRecipientsException();
            }
            else if (recipients.Count == 1)
            {
                return recipients.FirstOrDefault()?.EmailAddress.Name + (isWithEmailDetail ? ": " + recipients.FirstOrDefault()?.EmailAddress.Address : string.Empty);
            }

            var result = recipients.FirstOrDefault()?.EmailAddress.Name + (isWithEmailDetail ? ": " + recipients.FirstOrDefault()?.EmailAddress.Address : string.Empty);
            for (var i = 1; i < recipients.Count; i++)
            {
                if (i == recipients.Count - 1)
                {
                    result += string.Format(CommonStrings.SeparatorFormat, CommonStrings.And) + recipients[i].EmailAddress.Name + (isWithEmailDetail ? ": " + recipients[i].EmailAddress.Address : string.Empty);
                }
                else
                {
                    result += ", " + recipients[i].EmailAddress.Name + ": " + recipients[i].EmailAddress.Address;
                }
            }

            return result;
        }

        protected string GetSelectPromptString(PromptOptions selectOption, bool containNumbers)
        {
            var result = string.Empty;
            for (var i = 0; i < selectOption.Choices.Count; i++)
            {
                var choice = selectOption.Choices[i];
                result += "  ";
                if (containNumbers)
                {
                    result += i + 1 + "-";
                }

                result += choice.Value + "\r\n";
            }

            return result;
        }

        protected async Task<bool> GetPreviewSubjectAsync(WaterfallStepContext sc, string actionType, CancellationToken cancellationToken)
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                var focusedMessage = state.Message.FirstOrDefault();

                switch (actionType)
                {
                    case Actions.Reply:
                        state.Subject = focusedMessage.Subject.ToLower().StartsWith(EmailCommonStrings.Reply) ? focusedMessage.Subject : string.Format(EmailCommonStrings.ReplyReplyFormat, focusedMessage?.Subject);
                        state.FindContactInfor.Contacts = focusedMessage.ToRecipients.ToList();
                        break;
                    case Actions.Forward:
                        state.Subject = focusedMessage.Subject.ToLower().StartsWith(EmailCommonStrings.Forward) ? focusedMessage.Subject : string.Format(EmailCommonStrings.ForwardReplyFormat, focusedMessage?.Subject);
                        break;
                    case Actions.Send:
                    default:
                        break;
                }

                return true;
            }
            catch
            {
                // todo: should log the exception.
                return false;
            }
        }

        protected (List<Message>, EmailSearchType) FilterMessages(List<Message> messages, string searchSender, string searchSubject, string searchUserInput)
        {
            if ((searchSender == null) && (searchSubject == null) && (searchUserInput == null))
            {
                return (messages, EmailSearchType.None);
            }

            var searchType = EmailSearchType.None;

            // Get display messages
            var displayMessages = new List<Message>();
            for (var i = 0; i < messages.Count(); i++)
            {
                var messageSender = messages[i].Sender?.EmailAddress?.Name?.ToLowerInvariant();
                var messageSubject = messages[i].Subject?.ToLowerInvariant();

                if (messageSender != null)
                {
                    if ((searchType == EmailSearchType.None) || (searchType == EmailSearchType.SearchByContact))
                    {
                        if (((searchSender != null) && messageSender.Contains(searchSender))
                        || ((searchUserInput != null) && messageSender.Contains(searchUserInput)))
                        {
                            displayMessages.Add(messages[i]);

                            searchType = EmailSearchType.SearchByContact;
                            if (searchSender == null)
                            {
                                searchSender = searchUserInput;
                            }

                            searchSubject = null;
                            continue;
                        }
                    }
                }

                if (messageSubject != null)
                {
                    if ((searchType == EmailSearchType.None) || (searchType == EmailSearchType.SearchBySubject))
                    {
                        if (((searchSubject != null) && messageSubject.Contains(searchSubject))
                        || ((searchUserInput != null) && messageSubject.Contains(searchUserInput)))
                        {
                            displayMessages.Add(messages[i]);

                            searchType = EmailSearchType.SearchBySubject;
                            if (searchSubject == null)
                            {
                                searchSubject = searchUserInput;
                            }

                            searchSender = null;
                            continue;
                        }
                    }
                }
            }

            return (displayMessages, searchType);
        }

        protected async Task<(List<Message>, int, int)> GetMessagesAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var result = new List<Message>();

            var pageSize = ConfigData.GetInstance().MaxDisplaySize;
            var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
            sc.Context.TurnState.TryGetValue(StateProperties.APIToken, out var token);
            var serivce = ServiceManager.InitMailService(token as string, state.GetUserTimeZone(), state.MailSourceType);

            var isUnreadOnly = state.IsUnreadOnly;
            var isImportant = state.IsImportant;
            var startDateTime = state.StartDateTime;
            var endDateTime = state.EndDateTime;
            var directlyToMe = state.DirectlyToMe;
            var skip = state.ShowEmailIndex * pageSize;
            string mailAddress = null;

            // Get user message.
            result = await serivce.GetMyMessagesAsync(startDateTime, endDateTime, isUnreadOnly, isImportant, directlyToMe, mailAddress);

            // Filter messages
            var searchSender = state.GeneralSenderName?.ToLowerInvariant();
            var searchSubject = state.GeneralSearchTexts?.ToLowerInvariant();
            var searchType = EmailSearchType.None;
            (result, searchType) = FilterMessages(result, searchSender, searchSubject, null);

            if (searchType == EmailSearchType.SearchByContact)
            {
                state.GeneralSearchTexts = null;
            }
            else if (searchType == EmailSearchType.SearchBySubject)
            {
                state.GeneralSenderName = null;
            }

            // Go back to last page if next page didn't get anything
            if (skip >= result.Count)
            {
                skip = (state.ShowEmailIndex - 1) * pageSize;
            }

            // get messages for current page
            var filteredResult = new List<Message>();
            var importantEmailCount = 0;
            for (var i = 0; i < result.Count; i++)
            {
                if (result[i].Importance.HasValue && result[i].Importance.Value == Importance.High)
                {
                    importantEmailCount++;
                }

                if (skip > 0)
                {
                    skip--;
                }
                else
                {
                    if (filteredResult.Count < pageSize)
                    {
                        filteredResult.Add(result[i]);
                    }
                }
            }

            return (filteredResult, result.Count, importantEmailCount);
        }

        protected async Task ShowMailListAsync(WaterfallStepContext sc, List<Message> messages, int totalCount, int importantCount, CancellationToken cancellationToken = default(CancellationToken))
        {
            var updatedMessages = new List<Message>();
            var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

            var cards = new List<Card>();
            var emailList = new List<EmailCardData>();

            var taskList = new Task<string>[messages.Count()];
            for (int i = 0; i < messages.Count(); i++)
            {
                taskList[i] = GetUserPhotoUrlAsync(sc.Context, messages[i].Sender.EmailAddress, cancellationToken);
            }

            Task.WaitAll(taskList);

            for (int i = 0; i < messages.Count(); i++)
            {
                var message = messages[i];
                var nameListString = DisplayHelper.ToDisplayRecipientsString_Summay(message.ToRecipients);

                var emailCard = new EmailCardData
                {
                    Subject = message.Subject,
                    Sender = message.Sender.EmailAddress.Name,
                    NameList = string.Format(EmailCommonStrings.ToFormat, nameListString),
                    EmailContent = message.BodyPreview,
                    EmailLink = message.WebLink,
                    ReceivedDateTime = message.ReceivedDateTime == null
                    ? CommonStrings.NotAvailable
                    : message.ReceivedDateTime.Value.UtcDateTime.ToOverallRelativeString(state.GetUserTimeZone()),
                    Speak = SpeakHelper.ToSpeechEmailDetailOverallString(message, state.GetUserTimeZone()),
                    SenderIcon = taskList[i].Result
                };

                var isImportant = message.Importance != null && message.Importance == Importance.High;
                var hasAttachment = message.HasAttachments.HasValue && message.HasAttachments.Value;
                if (isImportant && hasAttachment)
                {
                    emailCard.AdditionalIcon1 = AdaptiveCardHelper.ImportantIcon;
                    emailCard.AdditionalIcon2 = AdaptiveCardHelper.AttachmentIcon;
                }
                else if (isImportant)
                {
                    emailCard.AdditionalIcon1 = AdaptiveCardHelper.ImportantIcon;
                }
                else if (hasAttachment)
                {
                    emailCard.AdditionalIcon1 = AdaptiveCardHelper.AttachmentIcon;
                }

                cards.Add(new Card("EmailOverviewItem", emailCard));
                emailList.Add(emailCard);
                updatedMessages.Add(message);
            }

            var tokens = new StringDictionary
            {
                { "TotalCount", totalCount.ToString() },
                { "EmailListDetails", SpeakHelper.ToSpeechEmailListString(updatedMessages, state.GetUserTimeZone(), ConfigData.GetInstance().MaxReadSize) }
            };

            var avator = await GetMyPhotoUrlAsync(sc.Context, cancellationToken);

            var maxPage = (totalCount / ConfigData.GetInstance().MaxDisplaySize) + (totalCount % ConfigData.GetInstance().MaxDisplaySize > 0 ? 1 : 0) - 1;
            var validShowEmailIndex = (state.ShowEmailIndex < 0) ? 0 : state.ShowEmailIndex;
            validShowEmailIndex = (validShowEmailIndex > maxPage) ? maxPage : validShowEmailIndex;
            var startIndex = (validShowEmailIndex * ConfigData.GetInstance().MaxDisplaySize) + 1;
            var endIndex = (startIndex + ConfigData.GetInstance().MaxDisplaySize - 1) > totalCount ? totalCount : (startIndex + ConfigData.GetInstance().MaxDisplaySize - 1);
            var overviewData = new EmailOverviewData()
            {
                Description = EmailCommonStrings.YourEmail,
                AvatorIcon = avator,
                TotalMessageNumber = totalCount.ToString(),
                HighPriorityMessagesNumber = importantCount.ToString(),
                Now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, state.GetUserTimeZone()).ToString(EmailCommonStrings.GeneralDateFormat),
                MailSourceType = string.Format(EmailCommonStrings.Source, AdaptiveCardHelper.GetSourceType(state.MailSourceType)),
                EmailIndexer = string.Format(
                    EmailCommonStrings.PageIndexerFormat,
                    startIndex.ToString(),
                    endIndex.ToString(),
                    totalCount.ToString()),
                EmailList = emailList
            };

            var overviewCard = GetDivergedCardName(sc.Context, "EmailOverviewCard");
            if ((state.SenderName != null) || (state.GeneralSenderName != null))
            {
                overviewData.Description = string.Format(EmailCommonStrings.SearchBySender, state.SenderName ?? state.GeneralSenderName);
                overviewCard = GetDivergedCardName(sc.Context, "EmailOverviewByCondition");
            }
            else if ((state.SearchTexts != null) || (state.GeneralSearchTexts != null))
            {
                overviewData.Description = string.Format(EmailCommonStrings.SearchBySubject, state.SearchTexts ?? state.GeneralSearchTexts);
                overviewCard = GetDivergedCardName(sc.Context, "EmailOverviewByCondition");
            }

            var reply = TemplateManager.GenerateActivityForLocale(
                        EmailSharedResponses.ShowEmailPromptWithFirstLastPrefix,
                        new
                        {
                            totalCount = totalCount.ToString(),
                            emailListDetails = SpeakHelper.ToSpeechEmailListString(updatedMessages, state.GetUserTimeZone(), ConfigData.GetInstance().MaxReadSize),
                            showEmailIndex = state.ShowEmailIndex,
                            showEmailCount = updatedMessages.Count,
                            maxEmailPage = maxPage,
                            emailOverview = overviewData
                        });

            if (state.ShowEmailIndex < 0)
            {
                state.ShowEmailIndex = 0;
            }
            else if (state.ShowEmailIndex > maxPage)
            {
                state.ShowEmailIndex--;
            }

            await sc.Context.SendActivityAsync(reply, cancellationToken);
            return;
        }

        protected async Task<string> GetMyPhotoUrlAsync(ITurnContext context, CancellationToken cancellationToken)
        {
            var state = await EmailStateAccessor.GetAsync(context, cancellationToken: cancellationToken);
            context.TurnState.TryGetValue(StateProperties.APIToken, out var token);
            var service = ServiceManager.InitUserService(token as string, state.GetUserTimeZone(), state.MailSourceType);

            try
            {
                var user = await service.GetMeAsync();
                if (user != null && !string.IsNullOrEmpty(user.Photo))
                {
                    return user.Photo;
                }

                // return default value
                return string.Format(AdaptiveCardHelper.DefaultAvatarIconPathFormat, AdaptiveCardHelper.DefaultMe);
            }
            catch (Exception)
            {
                // won't clear conversation state hear, because sometime use api is not available, like user msa account.
                return string.Format(AdaptiveCardHelper.DefaultAvatarIconPathFormat, AdaptiveCardHelper.DefaultMe);
            }
        }

        protected async Task<string> GetUserPhotoUrlAsync(ITurnContext context, EmailAddress email, CancellationToken cancellationToken)
        {
            var state = await EmailStateAccessor.GetAsync(context, cancellationToken: cancellationToken);
            context.TurnState.TryGetValue(StateProperties.APIToken, out var token);
            var service = ServiceManager.InitUserService(token as string, state.GetUserTimeZone(), state.MailSourceType);
            var displayName = email.Name ?? email.Address;

            try
            {
                var url = await service.GetPhotoAsync(email.Address);
                if (!string.IsNullOrEmpty(url))
                {
                    return url;
                }

                // return default value
                return string.Format(AdaptiveCardHelper.DefaultAvatarIconPathFormat, displayName);
            }
            catch (Exception)
            {
                return string.Format(AdaptiveCardHelper.DefaultAvatarIconPathFormat, displayName);
            }
        }

        protected async Task<string> GetPhotoByIndexAsync(ITurnContext context, IEnumerable<Recipient> recipients, int index, CancellationToken cancellationToken)
        {
            if (recipients.Count() <= index)
            {
                return AdaptiveCardHelper.DefaultIcon;
            }

            return await GetUserPhotoUrlAsync(context, recipients.ElementAt(index).EmailAddress, cancellationToken);
        }

        protected async Task<EmailCardData> ProcessRecipientPhotoUrlAsync(ITurnContext context, EmailCardData data, IEnumerable<Recipient> recipients, CancellationToken cancellationToken)
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(context, cancellationToken: cancellationToken);

                if (recipients == null || recipients.Count() == 0)
                {
                    throw new Exception("No recipient!");
                }

                var taskList = new Task<string>[AdaptiveCardHelper.MaxDisplayRecipientNum];
                for (int i = 0; i < taskList.Count(); i++)
                {
                    taskList[i] = GetPhotoByIndexAsync(context, recipients, i, cancellationToken);
                }

                Task.WaitAll(taskList);

                data.RecipientIcon0 = taskList[0].Result;
                data.RecipientIcon1 = taskList[1].Result;
                data.RecipientIcon2 = taskList[2].Result;
                data.RecipientIcon3 = taskList[3].Result;
                data.RecipientIcon4 = taskList[4].Result;

                if (recipients.Count() > AdaptiveCardHelper.MaxDisplayRecipientNum)
                {
                    // the last recipient turns into number
                    var additionalNumber = recipients.Count() - AdaptiveCardHelper.MaxDisplayRecipientNum + 1;
                    data.AdditionalRecipientNumber = additionalNumber.ToString();
                }

                // return default value
                return data;
            }
            catch (ServiceException)
            {
                return null;
            }
        }

        protected async Task ClearConversationStateAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                var skillOptions = (EmailSkillDialogOptions)sc.Options;
                var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (skillOptions == null || !skillOptions.SubFlowMode)
                {
                    state.Clear();
                }
                else
                {
                    state.PartialClear();
                }
            }
            catch (Exception)
            {
                // todo : should log error here.
                throw;
            }
        }

        protected async Task ClearAllStateAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                await DialogStateAccessor.DeleteAsync(sc.Context, cancellationToken);
                await EmailStateAccessor.SetAsync(sc.Context, new EmailSkillState(), cancellationToken);
            }
            catch (Exception)
            {
                // todo : should log error here.
                throw;
            }
        }

        protected async Task DigestFocusEmailAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await EmailStateAccessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
            var luisResult = sc.Context.TurnState.Get<EmailLuis>(StateProperties.EmailLuisResult);
            var userInput = sc.Context.Activity.Text;

            // Get focus message if any
            if (state.MessageList != null && state.UserSelectIndex >= 0 && state.UserSelectIndex < state.MessageList.Count())
            {
                state.Message.Clear();
                state.Message.Add(state.MessageList[state.UserSelectIndex]);
            }

            // Get focus message by title
            if (state.Message.Count <= 0 && userInput != null)
            {
                var subject = userInput;
                if (luisResult != null && luisResult.Entities != null && luisResult.Entities.EmailSubject != null)
                {
                    subject = luisResult.Entities.EmailSubject[0];
                }

                foreach (var message in state.MessageList)
                {
                    if (message.Subject.ToLower().Contains(subject.ToLower()))
                    {
                        state.Message.Add(message);
                    }
                }
            }

            // Get focus message by sender
            if (state.Message.Count <= 0 && userInput != null)
            {
                var contactName = userInput;
                if (luisResult != null && luisResult.Entities != null && luisResult.Entities.ContactName != null)
                {
                    contactName = luisResult.Entities.ContactName[0];
                }

                foreach (var message in state.MessageList)
                {
                    if (message.Sender.EmailAddress != null && message.Sender.EmailAddress.Name != null &&
                        message.Sender.EmailAddress.Name.ToLower().Contains(contactName.ToLower()))
                    {
                        state.Message.Add(message);
                        break;
                    }

                    if (message.Sender.EmailAddress != null && message.Sender.EmailAddress.Address != null &&
                        message.Sender.EmailAddress.Address.ToLower().Contains(contactName.ToLower()))
                    {
                        state.Message.Add(message);
                        break;
                    }
                }
            }
        }

        protected async Task DigestEmailLuisResultAsync(DialogContext dc, bool isBeginDialog, CancellationToken cancellationToken)
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(dc.Context, () => new EmailSkillState(), cancellationToken);
                var luisResult = dc.Context.TurnState.Get<EmailLuis>(StateProperties.EmailLuisResult);
                var generalResult = dc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResult);
                var intent = luisResult.TopIntent().intent;

                var entity = luisResult.Entities;
                var generalEntity = generalResult.Entities;

                if (entity != null)
                {
                    if (entity.ordinal != null)
                    {
                        try
                        {
                            var emailList = state.MessageList;
                            var value = entity.ordinal[0];
                            if (Math.Abs(value - (int)value) < double.Epsilon)
                            {
                                state.UserSelectIndex = (int)value - 1;
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    if (generalEntity != null && generalEntity.number != null && (entity.ordinal == null || entity.ordinal.Length == 0))
                    {
                        try
                        {
                            var emailList = state.MessageList;
                            var value = generalEntity.number[0];
                            if (Math.Abs(value - (int)value) < double.Epsilon)
                            {
                                state.UserSelectIndex = (int)value - 1;
                            }
                        }
                        catch
                        {
                            // ignored
                        }
                    }

                    if (!isBeginDialog)
                    {
                        return;
                    }

                    switch (intent)
                    {
                        case EmailLuis.Intent.CheckMessages:
                        case EmailLuis.Intent.SearchMessages:
                        case EmailLuis.Intent.ReadAloud:
                        case EmailLuis.Intent.ShowNext:
                        case EmailLuis.Intent.ShowPrevious:
                            {
                                // Get email search type
                                if (dc.Context.Activity.Text != null)
                                {
                                    var words = dc.Context.Activity.Text.Split(' ');
                                    {
                                        foreach (var word in words)
                                        {
                                            var lowerInput = word.ToLower();

                                            if (lowerInput.Contains(EmailCommonStrings.High) || lowerInput.Contains(EmailCommonStrings.Important))
                                            {
                                                state.IsImportant = true;
                                            }
                                            else if (lowerInput.Contains(EmailCommonStrings.Unread))
                                            {
                                                state.IsUnreadOnly = true;
                                            }
                                            else if (lowerInput.Contains(EmailCommonStrings.All))
                                            {
                                                state.IsUnreadOnly = false;
                                            }
                                        }
                                    }
                                }

                                if (entity.ContactName != null)
                                {
                                    foreach (var name in entity.ContactName)
                                    {
                                        if (!state.FindContactInfor.ContactsNameList.Contains(name))
                                        {
                                            state.FindContactInfor.ContactsNameList.Add(name);
                                        }
                                    }
                                }

                                if (entity.email != null)
                                {
                                    // As luis result for email address often contains extra spaces for word breaking
                                    // (e.g. send email to test@test.com, email address entity will be test @ test . com)
                                    // So use original user input as email address.
                                    var rawEntity = luisResult.Entities._instance.email;
                                    foreach (var emailAddress in rawEntity)
                                    {
                                        var email = luisResult.Text.Substring(emailAddress.StartIndex, emailAddress.EndIndex - emailAddress.StartIndex);
                                        if (Utilities.Util.IsEmail(email) && !state.FindContactInfor.ContactsNameList.Contains(email))
                                        {
                                            state.FindContactInfor.ContactsNameList.Add(email);
                                        }
                                    }
                                }

                                if (entity.SenderName != null)
                                {
                                    state.SenderName = entity.SenderName[0];
                                }

                                if (entity.SearchTexts != null)
                                {
                                    state.SearchTexts = entity.SearchTexts[0];
                                }
                                else if (entity.EmailSubject != null)
                                {
                                    state.SearchTexts = entity.EmailSubject[0];
                                }

                                break;
                            }

                        case EmailLuis.Intent.SendEmail:
                        case EmailLuis.Intent.Forward:
                        case EmailLuis.Intent.Reply:
                            {
                                if (entity.EmailSubject != null)
                                {
                                    state.Subject = entity.EmailSubject[0];
                                }

                                if (entity.Message != null)
                                {
                                    state.Content = entity.Message[0];
                                }

                                if (entity.ContactName != null)
                                {
                                    foreach (var name in entity.ContactName)
                                    {
                                        if (!state.FindContactInfor.ContactsNameList.Contains(name))
                                        {
                                            state.FindContactInfor.ContactsNameList.Add(name);
                                        }
                                    }
                                }

                                if (entity.email != null)
                                {
                                    // As luis result for email address often contains extra spaces for word breaking
                                    // (e.g. send email to test@test.com, email address entity will be test @ test . com)
                                    // So use original user input as email address.
                                    var rawEntity = luisResult.Entities._instance.email;
                                    foreach (var emailAddress in rawEntity)
                                    {
                                        var email = luisResult.Text.Substring(emailAddress.StartIndex, emailAddress.EndIndex - emailAddress.StartIndex);
                                        if (Utilities.Util.IsEmail(email) && !state.FindContactInfor.ContactsNameList.Contains(email))
                                        {
                                            state.FindContactInfor.ContactsNameList.Add(email);
                                        }
                                    }
                                }

                                if (entity.SenderName != null)
                                {
                                    state.SenderName = entity.SenderName[0];

                                    // Clear focus email if there is any.
                                    state.Message.Clear();
                                }

                                break;
                            }

                        default:
                            break;
                    }
                }
            }
            catch
            {
                // put log here
            }
        }

        protected bool IsReadMoreIntent(General.Intent? topIntent, string userInput)
        {
            var isReadMoreUserInput = userInput == null ? false : userInput.ToLowerInvariant().Contains(CommonStrings.More);
            return topIntent == General.Intent.ShowNext && isReadMoreUserInput;
        }

        // This method is called by any waterfall step that throws an exception to ensure consistency
        protected async Task HandleDialogExceptionsAsync(WaterfallStepContext sc, Exception ex, CancellationToken cancellationToken)
        {
            // send trace back to emulator
            var trace = new Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
            await sc.Context.SendActivityAsync(trace, cancellationToken);

            // log exception
            TelemetryClient.TrackException(ex, new Dictionary<string, string> { { nameof(sc.ActiveDialog), sc.ActiveDialog?.Id } });

            // send error message to bot user
            var activity = TemplateManager.GenerateActivityForLocale(EmailSharedResponses.EmailErrorMessage);
            await sc.Context.SendActivityAsync(activity, cancellationToken);

            // clear state
            await ClearAllStateAsync(sc, cancellationToken);
        }

        // This method is called by any waterfall step that throws a SkillException to ensure consistency
        protected async Task HandleDialogExceptionsAsync(WaterfallStepContext sc, SkillException ex, CancellationToken cancellationToken)
        {
            // send trace back to emulator
            var trace = new Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
            await sc.Context.SendActivityAsync(trace, cancellationToken);

            // log exception
            TelemetryClient.TrackException(ex, new Dictionary<string, string> { { nameof(sc.ActiveDialog), sc.ActiveDialog?.Id } });

            // send error message to bot user
            IMessageActivity activity = new Activity();
            if (ex.ExceptionType == SkillExceptionType.APIAccessDenied || ex.ExceptionType == SkillExceptionType.APIUnauthorized || ex.ExceptionType == SkillExceptionType.APIForbidden || ex.ExceptionType == SkillExceptionType.APIBadRequest)
            {
                activity = TemplateManager.GenerateActivityForLocale(EmailSharedResponses.EmailErrorMessageBotProblem);
            }
            else if (ex.ExceptionType == SkillExceptionType.AccountNotActivated)
            {
                activity = TemplateManager.GenerateActivityForLocale(EmailSharedResponses.EmailErrorMessageAccountProblem);
            }
            else
            {
                activity = TemplateManager.GenerateActivityForLocale(EmailSharedResponses.EmailErrorMessage);
            }

            await sc.Context.SendActivityAsync(activity, cancellationToken);

            // clear state
            await ClearAllStateAsync(sc, cancellationToken);
        }

        // Workaround until adaptive card renderer in teams is upgraded to v1.2
        protected string GetDivergedCardName(ITurnContext turnContext, string card)
        {
            if (Microsoft.Bot.Builder.Dialogs.Choices.Channel.GetChannelId(turnContext) == Channels.Msteams)
            {
                return card + ".1.0";
            }
            else
            {
                return card;
            }
        }

        [Serializable]
        protected class NoRecipientsException : Exception
        {
            public NoRecipientsException()
            {
            }

            public NoRecipientsException(string message)
                : base(message)
            {
            }

            public NoRecipientsException(string message, Exception innerException)
                : base(message, innerException)
            {
            }

            protected NoRecipientsException(SerializationInfo info, StreamingContext context)
                : base(info, context)
            {
            }
        }
    }
}