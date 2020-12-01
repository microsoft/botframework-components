// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ITSMSkill.Models;
using ITSMSkill.Prompts;
using ITSMSkill.Responses.Shared;
using ITSMSkill.Responses.Ticket;
using ITSMSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;

namespace ITSMSkill.Dialogs
{
    /// <summary>
    /// Dialog class for for Showing Ticket.
    /// </summary>
    public class ShowTicketDialog : SkillDialogBase
    {
        public ShowTicketDialog(
             IServiceProvider serviceProvider)
            : base(nameof(ShowTicketDialog), serviceProvider)
        {
            var showTicket = new WaterfallStep[]
            {
                ShowConstraintsAsync,

                // BeginShowTicketLoopAsync,
                // BeginShowAttributeLoopAsync,
                // LoopShowTicketAsync,
            };

            var showAttribute = new WaterfallStep[]
            {
                CheckAttributeAsync,
                InputAttributeAsync,
                SetAttributeAsync,
                UpdateSelectedAttributeAsync,
                LoopShowAttributeAsync,
            };

            var showTicketLoop = new WaterfallStep[]
            {
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                ShowTicketAsync,
                IfContinueShowAsync
            };

            var attributesForShow = new AttributeType[] { AttributeType.Number, AttributeType.Search, AttributeType.Urgency, AttributeType.State };

            var navigateYesNo = new HashSet<GeneralLuis.Intent>()
            {
                GeneralLuis.Intent.ShowNext,
                GeneralLuis.Intent.ShowPrevious,
                GeneralLuis.Intent.Confirm,
                GeneralLuis.Intent.Reject
            };

            AddDialog(new WaterfallDialog(Actions.ShowTicket, showTicket));
            AddDialog(new WaterfallDialog(Actions.ShowAttribute, showAttribute));
            AddDialog(new AttributeWithNoPrompt(Actions.ShowAttributePrompt, attributesForShow));
            AddDialog(new WaterfallDialog(Actions.ShowTicketLoop, showTicketLoop));
            AddDialog(new GeneralPrompt(Actions.ShowNavigatePrompt, navigateYesNo, StateAccessor, ShowNavigateValidatorAsync));

            InitialDialogId = Actions.ShowTicket;

            // never used
            // ConfirmAttributeResponse
            InputAttributeResponse = TicketResponses.ShowAttribute;
            InputAttributePrompt = Actions.ShowAttributePrompt;
        }

        protected async Task<DialogTurnResult> BeginShowAttributeLoopAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.BeginDialogAsync(Actions.ShowAttribute, cancellationToken: cancellationToken);
        }

        protected async Task<DialogTurnResult> BeginShowTicketLoopAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            state.PageIndex = -1;

            return await sc.BeginDialogAsync(Actions.ShowTicketLoop, cancellationToken: cancellationToken);
        }

        protected async Task<DialogTurnResult> ShowConstraintsAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);

            // always prompt for search
            state.AttributeType = AttributeType.None;

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(state.TicketNumber))
            {
                sb.AppendLine(string.Format(SharedStrings.TicketNumber, state.TicketNumber));
            }

            if (!string.IsNullOrEmpty(state.TicketTitle))
            {
                sb.AppendLine(string.Format(SharedStrings.Search, state.TicketTitle));
            }

            if (state.UrgencyLevel != UrgencyLevel.None)
            {
                sb.AppendLine(string.Format(SharedStrings.Urgency, state.UrgencyLevel.ToLocalizedString()));
            }

            if (state.TicketState != TicketState.None)
            {
                sb.AppendLine(string.Format(SharedStrings.TicketState, state.TicketState.ToLocalizedString()));
            }

            if (sb.Length == 0)
            {
                // await sc.Context.SendActivityAsync(ResponseManager.GetResponse(TicketResponses.ShowConstraintNone));
            }
            else
            {
                var token = new Dictionary<string, object>()
                {
                    { "Attributes", sb.ToString() }
                };

                await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(TicketResponses.ShowConstraints, token), cancellationToken);
            }

            state.PageIndex = -1;

            return await sc.ReplaceDialogAsync(Actions.ShowTicketLoop, cancellationToken: cancellationToken);
        }

        protected async Task<DialogTurnResult> LoopShowTicketAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.ReplaceDialogAsync(Actions.ShowTicket, cancellationToken: cancellationToken);
        }

        protected new async Task<DialogTurnResult> SetAttributeAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (sc.Result == null)
            {
                return await sc.ReplaceDialogAsync(Actions.ShowTicket, cancellationToken: cancellationToken);
            }

            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            state.AttributeType = (AttributeType)sc.Result;
            return await sc.NextAsync(cancellationToken: cancellationToken);
        }

        protected async Task<DialogTurnResult> LoopShowAttributeAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.ReplaceDialogAsync(Actions.ShowAttribute, cancellationToken: cancellationToken);
        }

        protected async Task<DialogTurnResult> ShowTicketAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            state.InterruptedIntent = ITSMLuis.Intent.None;

            bool firstDisplay = false;
            if (state.PageIndex == -1)
            {
                firstDisplay = true;
                state.PageIndex = 0;
            }

            var management = ServiceManager.CreateManagement(Settings, sc.Result as TokenResponse, state.ServiceCache);

            var urgencies = new List<UrgencyLevel>();
            if (state.UrgencyLevel != UrgencyLevel.None)
            {
                urgencies.Add(state.UrgencyLevel);
            }

            var states = new List<TicketState>();
            if (state.TicketState != TicketState.None)
            {
                if (state.TicketState == TicketState.Active)
                {
                    states.Add(TicketState.New);
                    states.Add(TicketState.InProgress);
                    states.Add(TicketState.OnHold);
                    states.Add(TicketState.Resolved);
                }
                else if (state.TicketState == TicketState.Inactive)
                {
                    states.Add(TicketState.Closed);
                    states.Add(TicketState.Canceled);
                }
                else
                {
                    states.Add(state.TicketState);
                }
            }

            var countResult = await management.CountTicket(query: state.TicketTitle, urgencies: urgencies, number: state.TicketNumber, states: states);

            if (!countResult.Success)
            {
                return await SendServiceErrorAndCancelAsync(sc, countResult, cancellationToken);
            }

            // adjust PageIndex
            int maxPage = Math.Max(0, (countResult.Tickets.Length - 1) / Settings.LimitSize);
            state.PageIndex = Math.Max(0, Math.Min(state.PageIndex, maxPage));

            // TODO handle consistency with count
            var result = await management.SearchTicket(state.PageIndex, query: state.TicketTitle, urgencies: urgencies, number: state.TicketNumber, states: states);

            if (!result.Success)
            {
                return await SendServiceErrorAndCancelAsync(sc, result, cancellationToken);
            }

            if (result.Tickets == null || result.Tickets.Length == 0)
            {
                if (firstDisplay)
                {
                    var options = new PromptOptions()
                    {
                        Prompt = TemplateManager.GenerateActivity(TicketResponses.TicketShowNone)
                    };

                    return await sc.PromptAsync(Actions.NavigateYesNoPrompt, options, cancellationToken);
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
                        Prompt = TemplateManager.GenerateActivity(TicketResponses.TicketEnd, token)
                    };

                    return await sc.PromptAsync(Actions.NavigateYesNoPrompt, options, cancellationToken);
                }
            }
            else
            {
                var cards = new List<Card>();
                foreach (var ticket in result.Tickets)
                {
                    cards.Add(GetTicketCard(sc.Context, state, ticket));
                }

                await sc.Context.SendActivityAsync(GetCardsWithIndicator(state.PageIndex, maxPage, cards), cancellationToken);

                var options = new PromptOptions()
                {
                    Prompt = GetNavigatePrompt(sc.Context, TicketResponses.TicketShow, state.PageIndex, maxPage),
                };

                return await sc.PromptAsync(Actions.ShowNavigatePrompt, options, cancellationToken);
            }
        }

        protected async Task<DialogTurnResult> IfContinueShowAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);

            // Skip in Action mode in ShowNavigateValidator
            if (state.InterruptedIntent == ITSMLuis.Intent.TicketClose)
            {
                return await sc.ReplaceDialogAsync(nameof(CloseTicketDialog), cancellationToken: cancellationToken);
            }
            else if (state.InterruptedIntent == ITSMLuis.Intent.TicketUpdate)
            {
                return await sc.ReplaceDialogAsync(nameof(UpdateTicketDialog), cancellationToken: cancellationToken);
            }
            else if (state.InterruptedIntent != ITSMLuis.Intent.None)
            {
                throw new Exception($"Invalid InterruptedIntent {state.InterruptedIntent}");
            }

            var intent = (GeneralLuis.Intent)sc.Result;
            if (intent == GeneralLuis.Intent.Reject)
            {
                return await sc.EndDialogAsync(await CreateActionResultAsync(sc.Context, true, cancellationToken), cancellationToken);
            }
            else if (intent == GeneralLuis.Intent.Confirm)
            {
                return await sc.ReplaceDialogAsync(Actions.ShowAttribute, cancellationToken: cancellationToken);
            }
            else if (intent == GeneralLuis.Intent.ShowNext)
            {
                state.PageIndex += 1;
                return await sc.ReplaceDialogAsync(Actions.ShowTicketLoop, cancellationToken: cancellationToken);
            }
            else if (intent == GeneralLuis.Intent.ShowPrevious)
            {
                state.PageIndex = Math.Max(0, state.PageIndex - 1);
                return await sc.ReplaceDialogAsync(Actions.ShowTicketLoop, cancellationToken: cancellationToken);
            }
            else
            {
                throw new Exception($"Invalid GeneralLuis.Intent {intent}");
            }
        }

        protected async Task<bool> ShowNavigateValidatorAsync(PromptValidatorContext<GeneralLuis.Intent> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
            {
                return true;
            }
            else
            {
                var state = await StateAccessor.GetAsync(promptContext.Context, () => new SkillState(), cancellationToken);
                if (state.IsAction)
                {
                    return false;
                }

                var result = promptContext.Context.TurnState.Get<ITSMLuis>(StateProperties.ITSMLuisResult);
                var topIntent = result.TopIntent();

                if (topIntent.score > 0.5 && (topIntent.intent == ITSMLuis.Intent.TicketClose || topIntent.intent == ITSMLuis.Intent.TicketUpdate))
                {
                    state.DigestLuisResult(result, topIntent.intent);
                    state.InterruptedIntent = topIntent.intent;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }
}
