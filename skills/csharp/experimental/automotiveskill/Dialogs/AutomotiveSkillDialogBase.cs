// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutomotiveSkill.Models;
using AutomotiveSkill.Responses.Shared;
using AutomotiveSkill.Services;
using AutomotiveSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;

namespace AutomotiveSkill.Dialogs
{
    public class AutomotiveSkillDialogBase : ComponentDialog
    {
        public AutomotiveSkillDialogBase(
            string dialogId,
            BotSettings settings,
            BotServices services,
            LocaleTemplateManager templateManager,
            ConversationState conversationState,
            IBotTelemetryClient telemetryClient)
            : base(dialogId)
        {
            Services = services;
            LocaleTemplateManager = templateManager;
            Accessor = conversationState.CreateProperty<AutomotiveSkillState>(nameof(AutomotiveSkillState));
            TelemetryClient = telemetryClient;
        }

        protected BotSettings Settings { get; set; }

        protected BotServices Services { get; set; }

        protected IStatePropertyAccessor<AutomotiveSkillState> Accessor { get; set; }

        protected LocaleTemplateManager LocaleTemplateManager { get; set; }

        // Shared steps
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }

        // Helpers
        protected Task DigestLuisResult(DialogContext dc)
        {
            return Task.CompletedTask;
        }

        // This method is called by any waterfall step that throws an exception to ensure consistency
        protected async Task HandleDialogExceptions(WaterfallStepContext sc, Exception ex)
        {
            // send trace back to emulator
            var trace = new Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
            await sc.Context.SendActivityAsync(trace);

            // log exception
            TelemetryClient.TrackException(ex, new Dictionary<string, string> { { nameof(sc.ActiveDialog), sc.ActiveDialog?.Id } });

            // send error message to bot user
            await sc.Context.SendActivityAsync(LocaleTemplateManager.GenerateActivityForLocale(AutomotiveSkillSharedResponses.ErrorMessage));
        }

        // Workaround until adaptive card renderer in teams is upgraded to v1.2
        protected string GetDivergedCardName(ITurnContext turnContext, string card)
        {
            if (Channel.GetChannelId(turnContext) == Channels.Msteams)
            {
                return card + ".1.0";
            }
            else
            {
                return card;
            }
        }
    }
}