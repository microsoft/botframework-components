// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Util;
using PhoneSkill.Common;
using PhoneSkill.Models;
using PhoneSkill.Models.Actions;
using PhoneSkill.Responses.OutgoingCall;
using PhoneSkill.Services;
using PhoneSkill.Services.Luis;
using PhoneSkill.Utilities;

namespace PhoneSkill.Dialogs
{
    public class OutgoingCallDialog : PhoneSkillDialogBase
    {
        private ContactFilter contactFilter;

        public OutgoingCallDialog(
            IServiceProvider serviceProvider)
            : base(nameof(OutgoingCallDialog), serviceProvider)
        {
            var outgoingCall = new List<WaterfallStep>
            {
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
            };

            var outgoingCallNoAuth = new List<WaterfallStep>
            {
                PromptForRecipientAsync,
                AskToSelectContactAsync,
                ConfirmChangeOfPhoneNumberTypeAsync,
                AskToSelectPhoneNumberAsync,
                ExecuteCallAsync,
            };

            foreach (var step in outgoingCallNoAuth)
            {
                outgoingCall.Add(step);
            }

            AddDialog(new WaterfallDialog(nameof(OutgoingCallDialog), outgoingCall));
            AddDialog(new WaterfallDialog(DialogIds.OutgoingCallNoAuth, outgoingCallNoAuth));

            AddDialog(new TextPrompt(DialogIds.RecipientPrompt, ValidateRecipientAsync));

            AddDialog(new ChoicePrompt(DialogIds.ContactSelection, ValidateContactChoiceAsync)
            {
                Style = ListStyle.List,
            });

            AddDialog(new ConfirmPrompt(DialogIds.PhoneNumberTypeConfirmation, ValidatePhoneNumberTypeConfirmationAsync)
            {
                Style = ListStyle.None,
            });

            AddDialog(new ChoicePrompt(DialogIds.PhoneNumberSelection, ValidatePhoneNumberChoiceAsync)
            {
                Style = ListStyle.List,
            });

            InitialDialogId = nameof(OutgoingCallDialog);

            contactFilter = new ContactFilter();
        }

        public async Task OnCancelAsync(DialogContext dialogContext, CancellationToken cancellationToken)
        {
            var state = await PhoneStateAccessor.GetAsync(dialogContext.Context, cancellationToken: cancellationToken);
            state.ClearExceptAuth();
        }

        public async Task OnLogoutAsync(DialogContext dialogContext, CancellationToken cancellationToken)
        {
            var state = await PhoneStateAccessor.GetAsync(dialogContext.Context, cancellationToken: cancellationToken);

            // When the user logs out, remove the login token and all their personal data from the state.
            state.Clear();
        }

        private async Task<DialogTurnResult> PromptForRecipientAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            try
            {
                var state = await PhoneStateAccessor.GetAsync(stepContext.Context, cancellationToken: cancellationToken);

                var contactProvider = GetContactProvider(state);
                await contactFilter.FilterAsync(state, contactProvider);

                var hasRecipient = await CheckRecipientAndExplainFailureToUserAsync(stepContext.Context, state, cancellationToken);
                if (hasRecipient)
                {
                    return await stepContext.NextAsync(cancellationToken: cancellationToken);
                }

                var prompt = TemplateManager.GenerateActivity(OutgoingCallResponses.RecipientPrompt);
                return await stepContext.PromptAsync(DialogIds.RecipientPrompt, new PromptOptions { Prompt = prompt }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(stepContext, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<bool> ValidateRecipientAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var state = await PhoneStateAccessor.GetAsync(promptContext.Context, cancellationToken: cancellationToken);

            var phoneResult = promptContext.Context.TurnState.Get<PhoneLuis>(StateProperties.PhoneLuisResultKey);
            contactFilter.OverrideEntities(state, phoneResult);

            var contactProvider = GetContactProvider(state);
            await contactFilter.FilterAsync(state, contactProvider);

            return await CheckRecipientAndExplainFailureToUserAsync(promptContext.Context, state, cancellationToken);
        }

        private async Task<DialogTurnResult> AskToSelectContactAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            try
            {
                var state = await PhoneStateAccessor.GetAsync(stepContext.Context, cancellationToken: cancellationToken);
                await contactFilter.FilterAsync(state, contactProvider: null);

                if (contactFilter.IsContactDisambiguated(state))
                {
                    return await stepContext.NextAsync(cancellationToken: cancellationToken);
                }

                var options = new PromptOptions();
                UpdateContactSelectionPromptOptions(options, state);

                return await stepContext.PromptAsync(DialogIds.ContactSelection, options, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(stepContext, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<bool> ValidateContactChoiceAsync(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            var state = await PhoneStateAccessor.GetAsync(promptContext.Context, cancellationToken: cancellationToken);
            if (contactFilter.IsContactDisambiguated(state))
            {
                return true;
            }

            var contactSelectionResult = await RunLuisAsync<ContactSelectionLuis>(promptContext.Context, "contactSelection", cancellationToken);
            contactFilter.OverrideEntities(state, contactSelectionResult);
            var (isFiltered, _) = await contactFilter.FilterAsync(state, contactProvider: null);
            if (contactFilter.IsContactDisambiguated(state))
            {
                return true;
            }
            else if (isFiltered)
            {
                UpdateContactSelectionPromptOptions(promptContext.Options, state);
                return false;
            }

            if (promptContext.Recognized.Value != null
                && promptContext.Recognized.Value.Index >= 0
                && promptContext.Recognized.Value.Index < state.ContactResult.Matches.Count)
            {
                state.ContactResult.Matches = new List<ContactCandidate>() { state.ContactResult.Matches[promptContext.Recognized.Value.Index] };
            }

            return contactFilter.IsContactDisambiguated(state);
        }

        private async Task<DialogTurnResult> ConfirmChangeOfPhoneNumberTypeAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            try
            {
                var state = await PhoneStateAccessor.GetAsync(stepContext.Context, cancellationToken: cancellationToken);
                var (isFiltered, hasPhoneNumberOfRequestedType) = await contactFilter.FilterAsync(state, contactProvider: null);

                if (hasPhoneNumberOfRequestedType
                    || !state.ContactResult.Matches.Any()
                    || !state.ContactResult.RequestedPhoneNumberType.Any())
                {
                    return await stepContext.NextAsync(cancellationToken: cancellationToken);
                }

                var notFoundTokens = new Dictionary<string, object>()
                {
                    { "contact", state.ContactResult.Matches[0].Name },
                    { "phoneNumberType", GetSpeakablePhoneNumberType(state.ContactResult.RequestedPhoneNumberType) },
                };
                var response = TemplateManager.GenerateActivity(OutgoingCallResponses.ContactHasNoPhoneNumberOfRequestedType, notFoundTokens);
                await stepContext.Context.SendActivityAsync(response, cancellationToken);

                if (state.ContactResult.Matches[0].PhoneNumbers.Count != 1)
                {
                    return await stepContext.NextAsync(cancellationToken: cancellationToken);
                }

                var confirmationTokens = new Dictionary<string, object>()
                {
                    { "phoneNumberType", GetSpeakablePhoneNumberType(state.ContactResult.Matches[0].PhoneNumbers[0].Type) },
                };
                var options = new PromptOptions();
                options.Prompt = TemplateManager.GenerateActivity(OutgoingCallResponses.ConfirmAlternativePhoneNumberType, confirmationTokens);
                return await stepContext.PromptAsync(DialogIds.PhoneNumberTypeConfirmation, options, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(stepContext, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<bool> ValidatePhoneNumberTypeConfirmationAsync(PromptValidatorContext<bool> promptContext, CancellationToken cancellationToken)
        {
            if (!promptContext.Recognized.Succeeded)
            {
                // The user said neither yes nor no.
                return false;
            }

            var state = await PhoneStateAccessor.GetAsync(promptContext.Context, cancellationToken: cancellationToken);
            if (promptContext.Recognized.Value)
            {
                // The user said yes.
                state.ContactResult.RequestedPhoneNumberType = state.ContactResult.Matches[0].PhoneNumbers[0].Type;
            }
            else
            {
                // The user said no.
                // We cannot restart the dialog from a validator function,
                // so we have to delay that until the next waterfall step is called.
                state.ClearExceptAuth();
            }

            return true;
        }

        private async Task<DialogTurnResult> AskToSelectPhoneNumberAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            try
            {
                var state = await PhoneStateAccessor.GetAsync(stepContext.Context, cancellationToken: cancellationToken);

                // If the user said 'no' to the PhoneNumberTypeConfirmation prompt,
                // then the state would have been cleared and we need to restart the whole dialog.
                if (!contactFilter.HasRecipient(state))
                {
                    return await stepContext.ReplaceDialogAsync(DialogIds.OutgoingCallNoAuth, cancellationToken: cancellationToken);
                }

                await contactFilter.FilterAsync(state, contactProvider: null);

                if (contactFilter.IsPhoneNumberDisambiguated(state))
                {
                    return await stepContext.NextAsync(cancellationToken: cancellationToken);
                }

                var options = new PromptOptions();
                UpdatePhoneNumberSelectionPromptOptions(options, state);

                return await stepContext.PromptAsync(DialogIds.PhoneNumberSelection, options, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(stepContext, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<bool> ValidatePhoneNumberChoiceAsync(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            var state = await PhoneStateAccessor.GetAsync(promptContext.Context, cancellationToken: cancellationToken);
            if (contactFilter.IsPhoneNumberDisambiguated(state))
            {
                return true;
            }

            var phoneNumberSelectionResult = await RunLuisAsync<PhoneNumberSelectionLuis>(promptContext.Context, "phoneNumberSelection", cancellationToken);
            contactFilter.OverrideEntities(state, phoneNumberSelectionResult);
            var (isFiltered, _) = await contactFilter.FilterAsync(state, contactProvider: null);
            if (contactFilter.IsPhoneNumberDisambiguated(state))
            {
                return true;
            }
            else if (isFiltered)
            {
                UpdatePhoneNumberSelectionPromptOptions(promptContext.Options, state);
                return false;
            }

            var phoneNumberList = state.ContactResult.Matches[0].PhoneNumbers;
            if (promptContext.Recognized.Value != null
                && promptContext.Recognized.Value.Index >= 0
                && promptContext.Recognized.Value.Index < phoneNumberList.Count)
            {
                state.ContactResult.Matches[0].PhoneNumbers = new List<PhoneNumber>() { phoneNumberList[promptContext.Recognized.Value.Index] };
            }

            return contactFilter.IsPhoneNumberDisambiguated(state);
        }

        private async Task<DialogTurnResult> ExecuteCallAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            try
            {
                var state = await PhoneStateAccessor.GetAsync(stepContext.Context, cancellationToken: cancellationToken);
                await contactFilter.FilterAsync(state, contactProvider: null);

                var templateId = OutgoingCallResponses.ExecuteCall;
                var tokens = new Dictionary<string, object>();
                var outgoingCall = new OutgoingCall
                {
                    Number = state.PhoneNumber,
                };
                if (state.ContactResult.Matches.Count == 1)
                {
                    tokens["contactOrPhoneNumber"] = state.ContactResult.Matches[0].Name;
                    outgoingCall.Contact = state.ContactResult.Matches[0];
                }
                else
                {
                    tokens["contactOrPhoneNumber"] = state.PhoneNumber;
                }

                if (state.ContactResult.RequestedPhoneNumberType.Any()
                    && state.ContactResult.Matches.Count == 1
                    && state.ContactResult.Matches[0].PhoneNumbers.Count == 1)
                {
                    templateId = OutgoingCallResponses.ExecuteCallWithPhoneNumberType;
                    tokens["phoneNumberType"] = GetSpeakablePhoneNumberType(state.ContactResult.Matches[0].PhoneNumbers[0].Type);
                }

                var response = TemplateManager.GenerateActivity(templateId, tokens);
                await stepContext.Context.SendActivityAsync(response, cancellationToken);

                await SendEventAsync(stepContext, outgoingCall, cancellationToken);

                ActionResult actionResult = null;
                if (state.IsAction)
                {
                    actionResult = new ActionResult() { ActionSuccess = true };
                }

                state.Clear();
                return await stepContext.EndDialogAsync(actionResult, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(stepContext, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private IContactProvider GetContactProvider(PhoneSkillState state)
        {
            if (state.SourceOfContacts == null)
            {
                // TODO Better error message to tell the bot developer where to specify the source.
                throw new Exception("Cannot retrieve contact list because no contact source specified.");
            }

            return ServiceManager.GetContactProvider(state.Token, state.SourceOfContacts.Value);
        }

        private async Task<bool> CheckRecipientAndExplainFailureToUserAsync(ITurnContext context, PhoneSkillState state, CancellationToken cancellationToken)
        {
            if (contactFilter.HasRecipient(state))
            {
                var contactsWithNoPhoneNumber = contactFilter.RemoveContactsWithNoPhoneNumber(state);

                if (contactFilter.HasRecipient(state))
                {
                    return true;
                }

                if (contactsWithNoPhoneNumber.Count == 1)
                {
                    var tokens = new Dictionary<string, object>()
                    {
                        { "contact", contactsWithNoPhoneNumber[0].Name },
                    };
                    var response = TemplateManager.GenerateActivity(OutgoingCallResponses.ContactHasNoPhoneNumber, tokens);
                    await context.SendActivityAsync(response, cancellationToken);
                }
                else
                {
                    var tokens = new Dictionary<string, object>()
                    {
                        { "contactName", state.ContactResult.SearchQuery },
                    };
                    var response = TemplateManager.GenerateActivity(OutgoingCallResponses.ContactsHaveNoPhoneNumber, tokens);
                    await context.SendActivityAsync(response, cancellationToken);
                }

                state.ContactResult.SearchQuery = string.Empty;
                return false;
            }

            if (state.ContactResult.SearchQuery.Any())
            {
                var tokens = new Dictionary<string, object>()
                {
                    { "contactName", state.ContactResult.SearchQuery },
                };
                var response = TemplateManager.GenerateActivity(OutgoingCallResponses.ContactNotFound, tokens);
                await context.SendActivityAsync(response, cancellationToken);
            }

            return false;
        }

        private void UpdateContactSelectionPromptOptions(PromptOptions options, PhoneSkillState state)
        {
            var templateId = OutgoingCallResponses.ContactSelection;
            var tokens = new Dictionary<string, object>
            {
                { "contactName", state.ContactResult.SearchQuery },
            };

            options.Choices = new List<Choice>();
            var searchQueryPreProcessed = contactFilter.PreProcess(state.ContactResult.SearchQuery);
            for (var i = 0; i < state.ContactResult.Matches.Count; ++i)
            {
                var item = state.ContactResult.Matches[i].Name;
                var synonyms = new List<string>
                {
                    item,
                    (i + 1).ToString(),
                };
                var choice = new Choice()
                {
                    Value = item,
                    Synonyms = synonyms,
                };
                options.Choices.Add(choice);

                if (!contactFilter.PreProcess(item).Contains(searchQueryPreProcessed, StringComparison.OrdinalIgnoreCase))
                {
                    templateId = OutgoingCallResponses.ContactSelectionWithoutName;
                    tokens.Remove("contactName");
                }
            }

            options.Prompt = TemplateManager.GenerateActivity(templateId, tokens);
        }

        private void UpdatePhoneNumberSelectionPromptOptions(PromptOptions options, PhoneSkillState state)
        {
            var templateId = OutgoingCallResponses.PhoneNumberSelection;
            var tokens = new Dictionary<string, object>
            {
                { "contact", state.ContactResult.Matches[0].Name },
            };

            options.Choices = new List<Choice>();
            var phoneNumberList = state.ContactResult.Matches[0].PhoneNumbers;
            var phoneNumberTypes = new HashSet<PhoneNumberType>();
            for (var i = 0; i < phoneNumberList.Count; ++i)
            {
                var phoneNumber = phoneNumberList[i];
                var speakableType = $"{GetSpeakablePhoneNumberType(phoneNumber.Type)}: {phoneNumber.Number}";
                var synonyms = new List<string>
                {
                    speakableType,
                    phoneNumber.Type.FreeForm,
                    phoneNumber.Number,
                    (i + 1).ToString(),
                };
                var choice = new Choice()
                {
                    Value = speakableType,
                    Synonyms = synonyms,
                };
                options.Choices.Add(choice);

                phoneNumberTypes.Add(phoneNumber.Type);
            }

            if (state.ContactResult.RequestedPhoneNumberType.Any() && phoneNumberTypes.Count == 1)
            {
                templateId = OutgoingCallResponses.PhoneNumberSelectionWithPhoneNumberType;
                tokens["phoneNumberType"] = GetSpeakablePhoneNumberType(phoneNumberTypes.First());
            }

            options.Prompt = TemplateManager.GenerateActivity(templateId, tokens);
        }

        private string GetSpeakablePhoneNumberType(PhoneNumberType phoneNumberType)
        {
            string speakableType;
            switch (phoneNumberType.Standardized)
            {
                case PhoneNumberType.StandardType.BUSINESS:
                    speakableType = "Business";
                    break;
                case PhoneNumberType.StandardType.HOME:
                    speakableType = "Home";
                    break;
                case PhoneNumberType.StandardType.MOBILE:
                    speakableType = "Mobile";
                    break;
                case PhoneNumberType.StandardType.NONE:
                default:
                    speakableType = phoneNumberType.FreeForm;
                    break;
            }

            return speakableType;
        }

        /// <summary>
        /// Send an event activity to communicate to the client which phone number to call.
        /// This event is meant to be processed by client code rather than shown to the user.
        /// </summary>
        /// <param name="stepContext">The WaterfallStepContext.</param>
        /// <param name="outgoingCall">The phone call to make.</param>
        /// <returns>A Task.</returns>
        private async Task SendEventAsync(WaterfallStepContext stepContext, OutgoingCall outgoingCall, CancellationToken cancellationToken)
        {
            var actionEvent = stepContext.Context.Activity.CreateReply();
            actionEvent.Type = ActivityTypes.Event;

            actionEvent.Name = "PhoneSkill.OutgoingCall";
            actionEvent.Value = outgoingCall;

            await stepContext.Context.SendActivityAsync(actionEvent, cancellationToken);
        }

        private static class DialogIds
        {
            public const string OutgoingCallNoAuth = "OutgoingCallNoAuth";
            public const string RecipientPrompt = "RecipientPrompt";
            public const string ContactSelection = "ContactSelection";
            public const string PhoneNumberTypeConfirmation = "PhoneNumberTypeConfirmation";
            public const string PhoneNumberSelection = "PhoneNumberSelection";
        }
    }
}
