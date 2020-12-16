// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ITSMSkill.Models;
using ITSMSkill.Models.Actions;
using ITSMSkill.Prompts;
using ITSMSkill.Responses.Knowledge;
using ITSMSkill.Responses.Shared;
using ITSMSkill.Services;
using ITSMSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;

namespace ITSMSkill.Dialogs
{
    /// <summary>
    /// Dialog class for for Knowledge.
    /// </summary>
    public class ShowKnowledgeDialog : SkillDialogBase
    {
        public ShowKnowledgeDialog(
             IServiceProvider serviceProvider)
            : base(nameof(ShowKnowledgeDialog), serviceProvider)
        {
            var showKnowledge = new WaterfallStep[]
            {
                CheckSearchAsync,
                InputSearchAsync,
                SetTitleAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                ShowKnowledgeLoopAsync,
                IfCreateTicketAsync,
                AfterIfCreateTicketAsync
            };

            var showKnowledgeLoop = new WaterfallStep[]
            {
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                ShowKnowledgeAsync,
                IfKnowledgeHelpAsync
            };

            AddDialog(new WaterfallDialog(Actions.ShowKnowledge, showKnowledge));
            AddDialog(new WaterfallDialog(Actions.ShowKnowledgeLoop, showKnowledgeLoop));

            InitialDialogId = Actions.ShowKnowledge;

            ShowKnowledgeNoResponse = KnowledgeResponses.KnowledgeShowNone;
            ShowKnowledgeEndResponse = KnowledgeResponses.KnowledgeEnd;
            ShowKnowledgeResponse = KnowledgeResponses.IfFindWanted;
            ShowKnowledgePrompt = Actions.NavigateYesNoPrompt;
            KnowledgeHelpLoop = Actions.ShowKnowledgeLoop;
        }

        protected async Task<DialogTurnResult> ShowKnowledgeLoopAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            state.PageIndex = -1;

            return await sc.BeginDialogAsync(Actions.ShowKnowledgeLoop, cancellationToken: cancellationToken);
        }

        protected async Task<DialogTurnResult> IfCreateTicketAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (sc.Result is EndFlowResult endFlow)
            {
                return await sc.EndDialogAsync(await CreateActionResultAsync(sc.Context, endFlow.Result, cancellationToken), cancellationToken);
            }

            // Skip create ticket in action mode
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
            if (state.IsAction)
            {
                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }

            var options = new PromptOptions()
            {
                Prompt = TemplateManager.GenerateActivity(KnowledgeResponses.IfCreateTicket)
            };

            return await sc.PromptAsync(nameof(ConfirmPrompt), options, cancellationToken);
        }

        protected async Task<DialogTurnResult> AfterIfCreateTicketAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            if ((bool)sc.Result)
            {
                var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState(), cancellationToken);
                state.DisplayExisting = false;

                // note that it replaces the active WaterfallDialog instead of ShowKnowledgeDialog
                return await sc.ReplaceDialogAsync(nameof(CreateTicketDialog), cancellationToken: cancellationToken);
            }
            else
            {
                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }
        }
    }
}
