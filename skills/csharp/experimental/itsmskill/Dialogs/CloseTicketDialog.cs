// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using ITSMSkill.Models;
using ITSMSkill.Responses.Ticket;
using ITSMSkill.Utilities;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace ITSMSkill.Dialogs
{
    /// <summary>
    /// Dialog class for for Closing Ticket.
    /// </summary>
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
                // Check if Error is UnAuthorized and logout user
                return await HandleAPIUnauthorizedError(sc, result, cancellationToken);
            }

            var card = GetTicketCard(sc.Context, state, result.Tickets[0]);

            await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(TicketResponses.TicketClosed, card, null), cancellationToken);
            return await sc.NextAsync(await CreateActionResultAsync(sc.Context, true, cancellationToken), cancellationToken);
        }
    }
}
