// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ITSMSkill.Models;
using ITSMSkill.Prompts;
using ITSMSkill.Responses.Shared;
using ITSMSkill.Responses.Ticket;
using ITSMSkill.Services;
using ITSMSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;

namespace ITSMSkill.Dialogs
{
    public class CloseTicketDialog : SkillDialogBase
    {
        public CloseTicketDialog(
             IServiceProvider serviceProvider)
            : base(nameof(CloseTicketDialog), serviceProvider)
        {
            var closeTicket = new WaterfallStep[]
            {
                BeginSetNumberThenIdAsync,
                CheckClosedAsync,
                CheckReasonAsync,
                InputReasonAsync,
                SetReasonAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                CloseTicketAsync
            };

            AddDialog(new WaterfallDialog(Actions.CloseTicket, closeTicket));

            InitialDialogId = Actions.CloseTicket;
        }

        protected async Task<DialogTurnResult> CheckClosedAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);

            if (state.TicketTarget.State == TicketState.Closed)
            {
                await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(TicketResponses.TicketAlreadyClosed), cancellationToken);
                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }

            return await sc.NextAsync(cancellationToken: cancellationToken);
        }

        protected async Task<DialogTurnResult> CloseTicketAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            var management = ServiceManager.CreateManagement(Settings, sc.Result as TokenResponse, state.ServiceCache);
            var result = await management.CloseTicket(id: state.Id, reason: state.CloseReason);

            if (!result.Success)
            {
                return await SendServiceErrorAndCancelAsync(sc, result, cancellationToken);
            }

            var card = GetTicketCard(sc.Context, result.Tickets[0]);

            await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(TicketResponses.TicketClosed, card, null), cancellationToken);
            return await sc.NextAsync(await CreateActionResultAsync(sc.Context, true, cancellationToken), cancellationToken);
        }
    }
}
