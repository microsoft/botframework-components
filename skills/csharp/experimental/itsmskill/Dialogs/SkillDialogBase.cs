// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ITSMSkill.Models;
using ITSMSkill.Models.Actions;
using ITSMSkill.Prompts;
using ITSMSkill.Responses.Shared;
using ITSMSkill.Responses.Ticket;
using ITSMSkill.Services;
using ITSMSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Authentication;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Extensions.DependencyInjection;

namespace ITSMSkill.Dialogs
{
    /// <summary>
    /// Dialog Base class.
    /// </summary>
    public class SkillDialogBase : ComponentDialog
    {
        public SkillDialogBase(
             string dialogId,
             IServiceProvider serviceProvider)
             : base(dialogId)
        {
            Settings = serviceProvider.GetService<BotSettings>();
            Services = serviceProvider.GetService<BotServices>();
            TemplateManager = serviceProvider.GetService<LocaleTemplateManager>();
            var conversationState = serviceProvider.GetService<ConversationState>();
            StateAccessor = conversationState.CreateProperty<SkillState>(nameof(SkillState));
            ServiceManager = serviceProvider.GetService<IServiceManager>();

            // NOTE: Uncomment the following if your skill requires authentication
            if (!Settings.OAuthConnections.Any())
            {
                throw new Exception("You must configure an authentication connection before using this component.");
            }

            AppCredentials oauthCredentials = null;
            if (Settings.OAuthCredentials != null &&
                !string.IsNullOrWhiteSpace(Settings.OAuthCredentials.MicrosoftAppId) &&
                !string.IsNullOrWhiteSpace(Settings.OAuthCredentials.MicrosoftAppPassword))
            {
                oauthCredentials = new MicrosoftAppCredentials(Settings.OAuthCredentials.MicrosoftAppId, Settings.OAuthCredentials.MicrosoftAppPassword);
            }

            AddDialog(new MultiProviderAuthDialog(Settings.OAuthConnections, null, oauthCredentials));

            var setSearch = new WaterfallStep[]
            {
                CheckSearchAsync,
                InputSearchAsync,
                SetTitleAsync
            };

            var setTitle = new WaterfallStep[]
            {
                CheckTitleAsync,
                InputTitleAsync,
                SetTitleAsync
            };

            var setDescription = new WaterfallStep[]
            {
                CheckDescriptionAsync,
                InputDescriptionAsync,
                SetDescriptionAsync
            };

            var setUrgency = new WaterfallStep[]
            {
                CheckUrgencyAsync,
                InputUrgencyAsync,
                SetUrgencyAsync
            };

            var setId = new WaterfallStep[]
            {
                CheckIdAsync,
                InputIdAsync,
                SetIdAsync
            };

            var setState = new WaterfallStep[]
            {
                CheckStateAsync,
                InputStateAsync,
                SetStateAsync
            };

            // TODO since number is ServiceNow specific regex, no need to check
            var setNumber = new WaterfallStep[]
            {
                InputTicketNumberAsync,
                SetTicketNumberAsync,
            };

            var setNumberThenId = new WaterfallStep[]
            {
                InputTicketNumberAsync,
                SetTicketNumberAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                SetIdFromNumberAsync,
            };

            var baseAuth = new WaterfallStep[]
            {
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                BeginInitialDialogAsync
            };

            var navigateYesNo = new HashSet<GeneralLuis.Intent>()
            {
                GeneralLuis.Intent.ShowNext,
                GeneralLuis.Intent.ShowPrevious,
                GeneralLuis.Intent.Confirm,
                GeneralLuis.Intent.Reject
            };

            var navigateNo = new HashSet<GeneralLuis.Intent>()
            {
                GeneralLuis.Intent.ShowNext,
                GeneralLuis.Intent.ShowPrevious,
                GeneralLuis.Intent.Reject
            };

            AddDialog(new TextPrompt(nameof(TextPrompt)));
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new ChoicePrompt(nameof(ChoicePrompt)));
            AddDialog(new TicketNumberPrompt(nameof(TicketNumberPrompt)));
            AddDialog(new WaterfallDialog(Actions.SetSearch, setSearch));
            AddDialog(new WaterfallDialog(Actions.SetTitle, setTitle));
            AddDialog(new WaterfallDialog(Actions.SetDescription, setDescription));
            AddDialog(new WaterfallDialog(Actions.SetUrgency, setUrgency));
            AddDialog(new WaterfallDialog(Actions.SetId, setId));
            AddDialog(new WaterfallDialog(Actions.SetState, setState));
            AddDialog(new WaterfallDialog(Actions.SetNumber, setNumber));
            AddDialog(new WaterfallDialog(Actions.SetNumberThenId, setNumberThenId));
            AddDialog(new WaterfallDialog(Actions.BaseAuth, baseAuth));
            AddDialog(new GeneralPrompt(Actions.NavigateYesNoPrompt, navigateYesNo, StateAccessor));
            AddDialog(new GeneralPrompt(Actions.NavigateNoPrompt, navigateNo, StateAccessor));

            base.InitialDialogId = Actions.BaseAuth;
        }

        protected BotSettings Settings { get; }

        protected BotServices Services { get; }

        protected IStatePropertyAccessor<SkillState> StateAccessor { get; }

        protected LocaleTemplateManager TemplateManager { get; }

        protected IServiceManager ServiceManager { get; }

        protected new string InitialDialogId { get; set; }

        protected string ConfirmAttributeResponse { get; set; }

        protected string InputAttributeResponse { get; set; }

        protected string InputAttributePrompt { get; set; }

        protected string ShowKnowledgeNoResponse { get; set; }

        protected string ShowKnowledgeHasResponse { get; set; }

        protected string ShowKnowledgeEndResponse { get; set; }

        protected string ShowKnowledgeResponse { get; set; }

        protected string ShowKnowledgePrompt { get; set; }

        protected string KnowledgeHelpLoop { get; set; }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }

        protected async Task<DialogTurnResult> GetAuthTokenAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                return await sc.PromptAsync(nameof(MultiProviderAuthDialog), new PromptOptions(), cancellationToken);
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

        protected async Task<DialogTurnResult> AfterGetAuthTokenAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);

                // When the user authenticates interactively we pass on the tokens/Response event which surfaces as a JObject
                // When the token is cached we get a TokenResponse object.
                if (sc.Result is ProviderTokenResponse providerTokenResponse)
                {
                    // Save TokenResponse to State
                    state.AccessTokenResponse = providerTokenResponse.TokenResponse;
                    return await sc.NextAsync(providerTokenResponse.TokenResponse, cancellationToken);
                }
                else
                {
                    await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(SharedResponses.AuthFailed), cancellationToken);
                    return await sc.CancelAllDialogsAsync(cancellationToken);
                }
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

        protected async Task<DialogTurnResult> BeginInitialDialogAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.ReplaceDialogAsync(InitialDialogId, sc.Options, cancellationToken: cancellationToken);
        }

        protected async Task<DialogTurnResult> CheckIdAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            if (string.IsNullOrEmpty(state.Id))
            {
                return await sc.NextAsync(false, cancellationToken);
            }
            else
            {
                var replacements = new Dictionary<string, object>
                {
                    { "Id", state.Id }
                };

                var options = new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivity(SharedResponses.ConfirmId, replacements)
                };

                return await sc.PromptAsync(nameof(ConfirmPrompt), options, cancellationToken);
            }
        }

        protected async Task<DialogTurnResult> InputIdAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            if (!(bool)sc.Result || string.IsNullOrEmpty(state.Id))
            {
                var options = new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivity(SharedResponses.InputId)
                };

                return await sc.PromptAsync(nameof(TextPrompt), options, cancellationToken);
            }
            else
            {
                return await sc.NextAsync(state.Id, cancellationToken);
            }
        }

        protected async Task<DialogTurnResult> SetIdAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            state.Id = (string)sc.Result;
            return await sc.NextAsync(cancellationToken: cancellationToken);
        }

        protected async Task<DialogTurnResult> CheckAttributeAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            if (state.AttributeType == AttributeType.None)
            {
                return await sc.NextAsync(false, cancellationToken);
            }
            else
            {
                var replacements = new Dictionary<string, object>
                {
                    { "Attribute", state.AttributeType.ToLocalizedString() }
                };

                var options = new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivity(ConfirmAttributeResponse, replacements)
                };

                return await sc.PromptAsync(nameof(ConfirmPrompt), options, cancellationToken);
            }
        }

        protected async Task<DialogTurnResult> InputAttributeAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            if (!(bool)sc.Result || state.AttributeType == AttributeType.None)
            {
                var options = new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivity(InputAttributeResponse)
                };

                return await sc.PromptAsync(InputAttributePrompt, options, cancellationToken);
            }
            else
            {
                return await sc.NextAsync(state.AttributeType, cancellationToken);
            }
        }

        protected async Task<DialogTurnResult> SetAttributeAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (sc.Result == null)
            {
                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }

            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            state.AttributeType = (AttributeType)sc.Result;
            return await sc.NextAsync(cancellationToken: cancellationToken);
        }

        protected async Task<DialogTurnResult> UpdateSelectedAttributeAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            var attribute = state.AttributeType;
            state.AttributeType = AttributeType.None;
            if (attribute == AttributeType.Description)
            {
                state.TicketDescription = null;
                return await sc.BeginDialogAsync(Actions.SetDescription, cancellationToken: cancellationToken);
            }
            else if (attribute == AttributeType.Title)
            {
                state.TicketTitle = null;
                return await sc.BeginDialogAsync(Actions.SetTitle, cancellationToken: cancellationToken);
            }
            else if (attribute == AttributeType.Search)
            {
                state.TicketTitle = null;
                return await sc.BeginDialogAsync(Actions.SetSearch, cancellationToken: cancellationToken);
            }
            else if (attribute == AttributeType.Urgency)
            {
                state.UrgencyLevel = UrgencyLevel.None;
                return await sc.BeginDialogAsync(Actions.SetUrgency, cancellationToken: cancellationToken);
            }
            else if (attribute == AttributeType.Id)
            {
                state.Id = null;
                return await sc.BeginDialogAsync(Actions.SetId, cancellationToken: cancellationToken);
            }
            else if (attribute == AttributeType.State)
            {
                state.TicketState = TicketState.None;
                return await sc.BeginDialogAsync(Actions.SetState, cancellationToken: cancellationToken);
            }
            else if (attribute == AttributeType.Number)
            {
                state.TicketNumber = null;
                return await sc.BeginDialogAsync(Actions.SetNumber, cancellationToken: cancellationToken);
            }
            else
            {
                throw new Exception($"Invalid AttributeType: {attribute}");
            }
        }

        // Actually Title
        protected async Task<DialogTurnResult> CheckSearchAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            if (string.IsNullOrEmpty(state.TicketTitle))
            {
                return await sc.NextAsync(false, cancellationToken);
            }
            else
            {
                var option = sc.Options as BaseOption;
                if (option != null && !option.ConfirmSearch)
                {
                    return await sc.NextAsync(true, cancellationToken);
                }

                var replacements = new Dictionary<string, object>
                {
                    { "Search", state.TicketTitle }
                };

                var options = new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivity(SharedResponses.ConfirmSearch, replacements)
                };

                return await sc.PromptAsync(nameof(ConfirmPrompt), options, cancellationToken);
            }
        }

        protected async Task<DialogTurnResult> InputSearchAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            if (!(bool)sc.Result || string.IsNullOrEmpty(state.TicketTitle))
            {
                var options = new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivity(SharedResponses.InputSearch)
                };

                return await sc.PromptAsync(nameof(TextPrompt), options, cancellationToken);
            }
            else
            {
                return await sc.NextAsync(state.TicketTitle, cancellationToken);
            }
        }

        protected async Task<DialogTurnResult> CheckTitleAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            if (string.IsNullOrEmpty(state.TicketTitle))
            {
                return await sc.NextAsync(false, cancellationToken);
            }
            else
            {
                var replacements = new Dictionary<string, object>
                {
                    { "Title", state.TicketTitle }
                };

                var options = new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivity(SharedResponses.ConfirmTitle, replacements)
                };

                return await sc.PromptAsync(nameof(ConfirmPrompt), options, cancellationToken);
            }
        }

        protected async Task<DialogTurnResult> InputTitleAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            if (!(bool)sc.Result || string.IsNullOrEmpty(state.TicketTitle))
            {
                var options = new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivity(SharedResponses.InputTitle)
                };

                return await sc.PromptAsync(nameof(TextPrompt), options, cancellationToken);
            }
            else
            {
                return await sc.NextAsync(state.TicketTitle, cancellationToken);
            }
        }

        protected async Task<DialogTurnResult> SetTitleAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            state.TicketTitle = (string)sc.Result;
            return await sc.NextAsync(cancellationToken: cancellationToken);
        }

        protected async Task<DialogTurnResult> CheckDescriptionAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // TODO in CreateTicketDialog, after display knowledge loop
            // Since we use EndDialogAsync to pass result, it needs to end here explicitly
            if (sc.Result is EndFlowResult endFlow)
            {
                return await sc.EndDialogAsync(await CreateActionResultAsync(sc.Context, endFlow.Result, cancellationToken), cancellationToken);
            }

            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            if (string.IsNullOrEmpty(state.TicketDescription))
            {
                return await sc.NextAsync(false, cancellationToken);
            }
            else
            {
                var replacements = new Dictionary<string, object>
                {
                    { "Description", state.TicketDescription }
                };

                var options = new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivity(SharedResponses.ConfirmDescription, replacements)
                };

                return await sc.PromptAsync(nameof(ConfirmPrompt), options, cancellationToken);
            }
        }

        protected async Task<DialogTurnResult> InputDescriptionAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            if (!(bool)sc.Result || string.IsNullOrEmpty(state.TicketDescription))
            {
                var options = new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivity(SharedResponses.InputDescription)
                };

                return await sc.PromptAsync(nameof(TextPrompt), options, cancellationToken);
            }
            else
            {
                return await sc.NextAsync(state.TicketDescription, cancellationToken);
            }
        }

        protected async Task<DialogTurnResult> SetDescriptionAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            state.TicketDescription = (string)sc.Result;
            return await sc.NextAsync(cancellationToken: cancellationToken);
        }

        protected async Task<DialogTurnResult> CheckReasonAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            if (string.IsNullOrEmpty(state.CloseReason))
            {
                return await sc.NextAsync(false, cancellationToken);
            }
            else
            {
                var replacements = new Dictionary<string, object>
                {
                    { "Reason", state.CloseReason }
                };

                var options = new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivity(SharedResponses.ConfirmReason, replacements)
                };

                return await sc.PromptAsync(nameof(ConfirmPrompt), options, cancellationToken);
            }
        }

        protected async Task<DialogTurnResult> InputReasonAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            if (!(bool)sc.Result || string.IsNullOrEmpty(state.CloseReason))
            {
                var options = new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivity(SharedResponses.InputReason)
                };

                return await sc.PromptAsync(nameof(TextPrompt), options, cancellationToken);
            }
            else
            {
                return await sc.NextAsync(state.CloseReason, cancellationToken);
            }
        }

        protected async Task<DialogTurnResult> SetReasonAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            state.CloseReason = (string)sc.Result;
            return await sc.NextAsync(cancellationToken: cancellationToken);
        }

        protected async Task<DialogTurnResult> InputTicketNumberAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            if (string.IsNullOrEmpty(state.TicketNumber))
            {
                var options = new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivity(SharedResponses.InputTicketNumber)
                };

                return await sc.PromptAsync(nameof(TicketNumberPrompt), options, cancellationToken);
            }
            else
            {
                return await sc.NextAsync(state.TicketNumber, cancellationToken);
            }
        }

        protected async Task<DialogTurnResult> SetTicketNumberAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            state.TicketNumber = (string)sc.Result;
            return await sc.NextAsync(cancellationToken: cancellationToken);
        }

        protected async Task<DialogTurnResult> SetIdFromNumberAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            var management = ServiceManager.CreateManagement(Settings, sc.Result as TokenResponse, state.ServiceCache);
            var result = await management.SearchTicket(0, number: state.TicketNumber);

            if (!result.Success)
            {
                return await SendServiceErrorAndCancelAsync(sc, result, cancellationToken);
            }

            if (result.Tickets == null || result.Tickets.Length == 0)
            {
                await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(TicketResponses.TicketFindNone), cancellationToken);
                return await sc.CancelAllDialogsAsync(cancellationToken);
            }

            if (result.Tickets.Length >= 2)
            {
                await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(TicketResponses.TicketDuplicateNumber), cancellationToken);
                return await sc.CancelAllDialogsAsync(cancellationToken);
            }

            state.TicketTarget = result.Tickets[0];
            state.Id = state.TicketTarget.Id;

            var card = GetTicketCard(sc.Context, state, state.TicketTarget, false);

            await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(TicketResponses.TicketTarget, card, null), cancellationToken);
            return await sc.NextAsync(cancellationToken: cancellationToken);
        }

        protected async Task<DialogTurnResult> BeginSetNumberThenIdAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.BeginDialogAsync(Actions.SetNumberThenId, cancellationToken: cancellationToken);
        }

        protected async Task<DialogTurnResult> CheckUrgencyAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            if (state.UrgencyLevel == UrgencyLevel.None)
            {
                return await sc.NextAsync(false, cancellationToken);
            }
            else
            {
                var replacements = new Dictionary<string, object>
                {
                    { "Urgency", state.UrgencyLevel.ToString() }
                };

                var options = new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivity(SharedResponses.ConfirmUrgency, replacements)
                };

                return await sc.PromptAsync(nameof(ConfirmPrompt), options, cancellationToken);
            }
        }

        protected async Task<DialogTurnResult> InputUrgencyAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            if (!(bool)sc.Result || state.UrgencyLevel == UrgencyLevel.None)
            {
                var options = new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivity(SharedResponses.InputUrgency),
                    Choices = new List<Choice>()
                    {
                        new Choice()
                        {
                            Value = UrgencyLevel.Low.ToLocalizedString()
                        },
                        new Choice()
                        {
                            Value = UrgencyLevel.Medium.ToLocalizedString()
                        },
                        new Choice()
                        {
                            Value = UrgencyLevel.High.ToLocalizedString()
                        }
                    }
                };

                return await sc.PromptAsync(nameof(ChoicePrompt), options, cancellationToken);
            }
            else
            {
                // use Index to skip localization
                return await sc.NextAsync(
                    new FoundChoice()
                    {
                        Index = (int)state.UrgencyLevel - 1,
                    },
                    cancellationToken);
            }
        }

        protected async Task<DialogTurnResult> SetUrgencyAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            state.UrgencyLevel = (UrgencyLevel)(((FoundChoice)sc.Result).Index + 1);
            return await sc.NextAsync(cancellationToken: cancellationToken);
        }

        protected async Task<DialogTurnResult> CheckStateAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            if (state.TicketState == TicketState.None)
            {
                return await sc.NextAsync(false, cancellationToken);
            }
            else
            {
                var replacements = new Dictionary<string, object>
                {
                    { "State", state.TicketState.ToString() }
                };

                var options = new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivity(SharedResponses.ConfirmState, replacements)
                };

                return await sc.PromptAsync(nameof(ConfirmPrompt), options, cancellationToken);
            }
        }

        protected async Task<DialogTurnResult> InputStateAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            if (!(bool)sc.Result || state.TicketState == TicketState.None)
            {
                var options = new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivity(SharedResponses.InputState),
                    Choices = new List<Choice>()
                    {
                        new Choice()
                        {
                            Value = TicketState.New.ToLocalizedString()
                        },
                        new Choice()
                        {
                            Value = TicketState.InProgress.ToLocalizedString()
                        },
                        new Choice()
                        {
                            Value = TicketState.OnHold.ToLocalizedString()
                        },
                        new Choice()
                        {
                            Value = TicketState.Resolved.ToLocalizedString()
                        },
                        new Choice()
                        {
                            Value = TicketState.Closed.ToLocalizedString()
                        },
                        new Choice()
                        {
                            Value = TicketState.Canceled.ToLocalizedString()
                        },
                        new Choice()
                        {
                            Value = TicketState.Active.ToLocalizedString()
                        },
                        new Choice()
                        {
                            Value = TicketState.Inactive.ToLocalizedString()
                        }
                    }
                };

                return await sc.PromptAsync(nameof(ChoicePrompt), options, cancellationToken);
            }
            else
            {
                // use Index to skip localization
                return await sc.NextAsync(
                    new FoundChoice()
                    {
                        Index = (int)state.TicketState - 1,
                    },
                    cancellationToken);
            }
        }

        protected async Task<DialogTurnResult> SetStateAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            state.TicketState = (TicketState)(((FoundChoice)sc.Result).Index + 1);
            return await sc.NextAsync(cancellationToken: cancellationToken);
        }

        protected async Task<DialogTurnResult> ShowKnowledgeAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);

            bool firstDisplay = false;
            if (state.PageIndex == -1)
            {
                firstDisplay = true;
                state.PageIndex = 0;
            }

            var management = ServiceManager.CreateManagement(Settings, sc.Result as TokenResponse, state.ServiceCache);

            var countResult = await management.CountKnowledge(state.TicketTitle);

            if (!countResult.Success)
            {
                return await SendServiceErrorAndCancelAsync(sc, countResult, cancellationToken);
            }

            // adjust PageIndex
            int maxPage = Math.Max(0, (countResult.Knowledges.Length - 1) / Settings.LimitSize);
            state.PageIndex = Math.Max(0, Math.Min(state.PageIndex, maxPage));

            // TODO handle consistency with count
            var result = await management.SearchKnowledge(state.TicketTitle, state.PageIndex);

            if (!result.Success)
            {
                return await SendServiceErrorAndCancelAsync(sc, result, cancellationToken);
            }

            if (result.Knowledges == null || result.Knowledges.Length == 0)
            {
                if (firstDisplay)
                {
                    if (!string.IsNullOrEmpty(ShowKnowledgeNoResponse))
                    {
                        await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(ShowKnowledgeNoResponse), cancellationToken);
                    }

                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }
                else
                {
                    // it is unlikely to happen now
                    var token = new Dictionary<string, object>()
                    {
                        { "Page", (state.PageIndex + 1).ToString() }
                    };

                    var options = new PromptOptions()
                    {
                        Prompt = TemplateManager.GenerateActivity(ShowKnowledgeEndResponse, token)
                    };

                    return await sc.PromptAsync(ShowKnowledgePrompt, options, cancellationToken);
                }
            }
            else
            {
                if (firstDisplay)
                {
                    if (!string.IsNullOrEmpty(ShowKnowledgeHasResponse))
                    {
                        await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(ShowKnowledgeHasResponse), cancellationToken);
                    }
                }

                var cards = new List<Card>();
                foreach (var knowledge in result.Knowledges)
                {
                    cards.Add(new Card()
                    {
                        Name = GetDivergedCardName(sc.Context, state, "Knowledge"),
                        Data = ConvertKnowledge(knowledge)
                    });
                }

                await sc.Context.SendActivityAsync(GetCardsWithIndicator(state.PageIndex, maxPage, cards), cancellationToken);

                var options = new PromptOptions()
                {
                    Prompt = GetNavigatePrompt(sc.Context, ShowKnowledgeResponse, state.PageIndex, maxPage),
                };

                return await sc.PromptAsync(ShowKnowledgePrompt, options, cancellationToken);
            }
        }

        protected async Task<DialogTurnResult> HandleAPIUnauthorizedError(WaterfallStepContext sc, TicketsResult ticketsResult, CancellationToken cancellationToken)
        {
            // Check if the error is UnAuthorized
            if (ticketsResult.Reason.Equals("Unauthorized"))
            {
                // Logout User
                var botAdapter = (BotFrameworkAdapter)sc.Context.Adapter;
                await botAdapter.SignOutUserAsync(sc.Context, Settings.OAuthConnections.FirstOrDefault().Name, null, cancellationToken);

                // Send Signout Message
                return await SignOutUser(sc);
            }
            else
            {
                return await SendServiceErrorAndCancel(sc, ticketsResult);
            }
        }

        protected async Task<DialogTurnResult> SendServiceErrorAndCancel(WaterfallStepContext sc, ResultBase result, CancellationToken cancellationToken = default(CancellationToken))
        {
            await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(SharedResponses.ServiceFailed), cancellationToken);
            return await sc.CancelAllDialogsAsync();
        }

        protected async Task<DialogTurnResult> SignOutUser(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(SharedResponses.SignOut), cancellationToken);
            return await sc.EndDialogAsync();
        }

        protected async Task<DialogTurnResult> IfKnowledgeHelpAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var intent = (GeneralLuis.Intent)sc.Result;
            if (intent == GeneralLuis.Intent.Confirm)
            {
                return await sc.EndDialogAsync(new EndFlowResult(true), cancellationToken);
            }
            else if (intent == GeneralLuis.Intent.Reject)
            {
                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }
            else if (intent == GeneralLuis.Intent.ShowNext)
            {
                var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
                state.PageIndex += 1;
                return await sc.ReplaceDialogAsync(KnowledgeHelpLoop, cancellationToken: cancellationToken);
            }
            else if (intent == GeneralLuis.Intent.ShowPrevious)
            {
                var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
                state.PageIndex = Math.Max(0, state.PageIndex - 1);
                return await sc.ReplaceDialogAsync(KnowledgeHelpLoop, cancellationToken: cancellationToken);
            }
            else
            {
                throw new Exception($"Invalid GeneralLuis.Intent ${intent}");
            }
        }

        // Validators
        protected Task<bool> TokenResponseValidatorAsync(PromptValidatorContext<Activity> pc, CancellationToken cancellationToken)
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

        protected Task<bool> AuthPromptValidatorAsync(PromptValidatorContext<TokenResponse> promptContext, CancellationToken cancellationToken)
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
        // This method is called by any waterfall step that throws an exception to ensure consistency
        protected async Task HandleDialogExceptionsAsync(WaterfallStepContext sc, Exception ex, CancellationToken cancellationToken)
        {
            // send trace back to emulator
            var trace = new Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
            await sc.Context.SendActivityAsync(trace, cancellationToken);

            // log exception
            TelemetryClient.TrackException(ex, new Dictionary<string, string> { { nameof(sc.ActiveDialog), sc.ActiveDialog?.Id } });

            // send error message to bot user
            await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(SharedResponses.ErrorMessage), cancellationToken);

            // clear state
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            state.ClearLuisResult();
        }

        protected async Task<DialogTurnResult> SendServiceErrorAndCancelAsync(WaterfallStepContext sc, ResultBase result, CancellationToken cancellationToken)
        {
            var errorReplacements = new Dictionary<string, object>
            {
                { "Error", result.ErrorMessage }
            };
            await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(SharedResponses.ServiceFailed, errorReplacements), cancellationToken);
            return await sc.CancelAllDialogsAsync(cancellationToken);
        }

        protected string GetNavigateString(int page, int maxPage)
        {
            if (page == 0)
            {
                if (maxPage == 0)
                {
                    return string.Empty;
                }
                else
                {
                    return SharedStrings.GoForward;
                }
            }
            else if (page == maxPage)
            {
                return SharedStrings.GoPrevious;
            }
            else
            {
                return SharedStrings.GoBoth;
            }
        }

        protected IList<Choice> GetNavigateList(int page, int maxPage)
        {
            var result = new List<Choice>() { new Choice(SharedStrings.YesUtterance), new Choice(SharedStrings.NoUtterance) };
            if (page == 0)
            {
                if (maxPage == 0)
                {
                }
                else
                {
                    result.Add(new Choice(SharedStrings.GoForwardUtterance));
                }
            }
            else if (page == maxPage)
            {
                result.Add(new Choice(SharedStrings.GoPreviousUtterance));
            }
            else
            {
                result.Add(new Choice(SharedStrings.GoForwardUtterance));
                result.Add(new Choice(SharedStrings.GoPreviousUtterance));
            }

            return result;
        }

        protected Activity GetNavigatePrompt(ITurnContext context, string response, int pageIndex, int maxPage)
        {
            var token = new Dictionary<string, object>()
            {
                { "Navigate", GetNavigateString(pageIndex, maxPage) },
            };

            var prompt = TemplateManager.GenerateActivity(response, token);

            return ChoiceFactory.ForChannel(context.Activity.ChannelId, GetNavigateList(pageIndex, maxPage), prompt.Text, prompt.Speak) as Activity;
        }

        protected Activity GetCardsWithIndicator(int pageIndex, int maxPage, IList<Card> cards)
        {
            if (maxPage == 0)
            {
                return TemplateManager.GenerateActivity(cards.Count == 1 ? SharedResponses.ResultIndicator : SharedResponses.ResultsIndicator, cards);
            }
            else
            {
                var token = new Dictionary<string, object>()
                {
                    { "Current", (pageIndex + 1).ToString() },
                    { "Total", (maxPage + 1).ToString() },
                };

                return TemplateManager.GenerateActivity(SharedResponses.PageIndicator, cards, token);
            }
        }

        protected Card GetTicketCard(ITurnContext turnContext, SkillState state, Ticket ticket, bool showButton = true)
        {
            var name = showButton ? (ticket.State != TicketState.Closed ? "TicketUpdateClose" : "TicketUpdate") : "Ticket";

            return new Card
            {
                Name = GetDivergedCardName(turnContext, state, name),
                Data = ConvertTicket(ticket)
            };
        }

        protected TicketCard ConvertTicket(Ticket ticket)
        {
            var card = new TicketCard()
            {
                Title = ticket.Title,
                Description = ticket.Description,
                UrgencyLevel = string.Format(SharedStrings.Urgency, ticket.Urgency.ToLocalizedString()),
                State = string.Format(SharedStrings.TicketState, ticket.State.ToLocalizedString()),
                OpenedTime = string.Format(SharedStrings.OpenedAt, ticket.OpenedTime.ToString()),
                Id = string.Format(SharedStrings.ID, ticket.Id),
                ResolvedReason = ticket.ResolvedReason,
                Speak = ticket.Description,
                Number = string.Format(SharedStrings.TicketNumber, ticket.Number),
                ActionUpdateTitle = SharedStrings.TicketActionUpdateTitle,
                ActionUpdateValue = string.Format(SharedStrings.TicketActionUpdateValue, ticket.Number),
                ProviderDisplayText = string.Format(SharedStrings.PoweredBy, ticket.Provider),
            };

            if (ticket.State != TicketState.Closed)
            {
                card.ActionCloseTitle = SharedStrings.TicketActionCloseTitle;
                card.ActionCloseValue = string.Format(SharedStrings.TicketActionCloseValue, ticket.Number);
            }

            return card;
        }

        protected KnowledgeCard ConvertKnowledge(Knowledge knowledge)
        {
            var card = new KnowledgeCard()
            {
                Id = string.Format(SharedStrings.ID, knowledge.Id),
                Title = knowledge.Title,
                UpdatedTime = string.Format(SharedStrings.UpdatedAt, knowledge.UpdatedTime.ToString()),
                Content = knowledge.Content,
                Speak = knowledge.Title,
                Number = string.Format(SharedStrings.TicketNumber, knowledge.Number),
                UrlTitle = SharedStrings.OpenKnowledge,
                UrlLink = knowledge.Url,
                ProviderDisplayText = string.Format(SharedStrings.PoweredBy, knowledge.Provider),
            };
            return card;
        }

        protected async Task<ActionResult> CreateActionResultAsync(ITurnContext context, bool success, CancellationToken cancellationToken)
        {
            var state = await StateAccessor.GetAsync(context, () => new SkillState(), cancellationToken);
            if (success && state.IsAction)
            {
                return new ActionResult(success);
            }
            else
            {
                return null;
            }
        }

        protected string GetDivergedCardName(ITurnContext turnContext, SkillState state, string card)
        {
            if (state.IsAction)
            {
                return card + ".pva";
            }

            if (Channel.GetChannelId(turnContext) == Channels.Msteams)
            {
                return card + ".1.0";
            }
            else
            {
                return card;
            }
        }

        protected static class Actions
        {
            public const string SetSearch = "SetSearch";
            public const string SetTitle = "SetTitle";
            public const string SetDescription = "SetDescription";
            public const string SetUrgency = "SetUrgency";
            public const string SetId = "SetId";
            public const string SetState = "SetState";
            public const string SetNumber = "SetNumber";
            public const string SetNumberThenId = "SetNumberThenId";

            public const string BaseAuth = "BaseAuth";

            public const string NavigateYesNoPrompt = "NavigateYesNoPrompt";
            public const string NavigateNoPrompt = "NavigateNoPrompt";

            public const string CreateTicket = "CreateTicket";
            public const string DisplayExisting = "DisplayExisting";

            public const string UpdateTicket = "UpdateTicket";
            public const string UpdateAttribute = "UpdateAttribute";
            public const string UpdateAttributePrompt = "UpdateAttributePrompt";

            public const string ShowTicket = "ShowTicket";
            public const string ShowAttribute = "ShowAttribute";
            public const string ShowAttributePrompt = "ShowAttributePrompt";
            public const string ShowTicketLoop = "ShowTicketLoop";
            public const string ShowNavigatePrompt = "ShowNavigatePrompt";

            public const string CloseTicket = "CloseTicket";

            public const string ShowKnowledge = "ShowKnowledge";
            public const string ShowKnowledgeLoop = "ShowKnowledgeLoop";

            public const string CreateTicketTeamsTaskModule = "CreateTickTeamsTaskModule";
            public const string CreateSubscriptionTaskModule = "CreateSubscriptionTaskModule";
        }
    }
}
