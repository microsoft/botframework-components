﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ITSMSkill.Models;
using ITSMSkill.Models.ServiceNow;
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
    public class UpdateTicketDialog : SkillDialogBase
    {
        public UpdateTicketDialog(
             IServiceProvider serviceProvider)
            : base(nameof(UpdateTicketDialog), serviceProvider)
        {
            var updateTicket = new WaterfallStep[]
            {
                BeginSetNumberThenIdAsync,
                UpdateAttributeLoopAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                UpdateTicketAsync
            };

            var updateAttribute = new WaterfallStep[]
            {
                ShowUpdatesAsync,
                CheckAttributeAsync,
                InputAttributeAsync,
                SetAttributeAsync,
                UpdateSelectedAttributeAsync,
                UpdateLoopAsync
            };

            var attributesForUpdate = new AttributeType[] { AttributeType.Title, AttributeType.Description, AttributeType.Urgency };

            AddDialog(new WaterfallDialog(Actions.UpdateTicket, updateTicket));
            AddDialog(new WaterfallDialog(Actions.UpdateAttribute, updateAttribute));
            AddDialog(new AttributeWithNoPrompt(Actions.UpdateAttributePrompt, attributesForUpdate));

            InitialDialogId = Actions.UpdateTicket;

            ConfirmAttributeResponse = TicketResponses.ConfirmUpdateAttribute;
            InputAttributeResponse = TicketResponses.UpdateAttribute;
            InputAttributePrompt = Actions.UpdateAttributePrompt;
        }

        protected async Task<DialogTurnResult> UpdateAttributeLoopAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await sc.BeginDialogAsync(Actions.UpdateAttribute, cancellationToken: cancellationToken);
        }

        protected async Task<DialogTurnResult> ShowUpdatesAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            var sb = new StringBuilder();

            if (!string.IsNullOrEmpty(state.TicketTitle))
            {
                sb.AppendLine(string.Format(SharedStrings.Title, state.TicketTitle));
            }

            if (!string.IsNullOrEmpty(state.TicketDescription))
            {
                sb.AppendLine(string.Format(SharedStrings.Description, state.TicketDescription));
            }

            if (state.UrgencyLevel != UrgencyLevel.None)
            {
                sb.AppendLine(string.Format(SharedStrings.Urgency, state.UrgencyLevel.ToLocalizedString()));
            }

            if (sb.Length == 0)
            {
                // await sc.Context.SendActivityAsync(ResponseManager.GetResponse(TicketResponses.ShowUpdateNone));
            }
            else
            {
                var token = new Dictionary<string, object>()
                {
                    { "Attributes", sb.ToString() }
                };
                await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(TicketResponses.ShowUpdates, token), cancellationToken);
            }

            return await sc.NextAsync(cancellationToken: cancellationToken);
        }

        protected async Task<DialogTurnResult> UpdateLoopAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);

            // state.AttributeType from Luis should be used first
            state.AttributeType = AttributeType.None;

            return await sc.ReplaceDialogAsync(Actions.UpdateAttribute, cancellationToken: cancellationToken);
        }

        protected async Task<DialogTurnResult> UpdateTicketAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);

            if (string.IsNullOrEmpty(state.TicketTitle) && string.IsNullOrEmpty(state.TicketDescription) && state.UrgencyLevel == UrgencyLevel.None)
            {
                await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(TicketResponses.TicketNoUpdate), cancellationToken);
                return await sc.NextAsync(cancellationToken: cancellationToken);
            }

            var management = ServiceManager.CreateManagement(Settings, sc.Result as TokenResponse, state.ServiceCache);
            var result = await management.UpdateTicket(state.Id, state.TicketTitle, state.TicketDescription, state.UrgencyLevel);

            if (!result.Success)
            {
                return await SendServiceErrorAndCancelAsync(sc, result, cancellationToken);
            }

            var card = GetTicketCard(sc.Context, result.Tickets[0]);

            await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(TicketResponses.TicketUpdated, card, null), cancellationToken);
            return await sc.NextAsync(await CreateActionResultAsync(sc.Context, true, cancellationToken), cancellationToken);
        }
    }
}
