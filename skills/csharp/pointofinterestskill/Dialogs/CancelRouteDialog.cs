﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Util;
using PointOfInterestSkill.Responses.CancelRoute;
using PointOfInterestSkill.Services;
using PointOfInterestSkill.Utilities;

namespace PointOfInterestSkill.Dialogs
{
    public class CancelRouteDialog : PointOfInterestDialogBase
    {
        public CancelRouteDialog(
            BotSettings settings,
            BotServices services,
            LocaleTemplateManager templateManager,
            ConversationState conversationState,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            IHttpContextAccessor httpContext)
            : base(nameof(CancelRouteDialog), settings, services, templateManager, conversationState, serviceManager, telemetryClient, httpContext)
        {
            TelemetryClient = telemetryClient;

            var cancelRoute = new WaterfallStep[]
            {
                CancelActiveRoute,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.CancelActiveRoute, cancelRoute) { TelemetryClient = telemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.CancelActiveRoute;
        }

        public async Task<DialogTurnResult> CancelActiveRoute(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.ActiveRoute != null)
                {
                    var replyMessage = TemplateManager.GenerateActivity(CancelRouteResponses.CancelActiveRoute);
                    await sc.Context.SendActivityAsync(replyMessage);
                    state.ActiveRoute = null;
                    state.Destination = null;
                }
                else
                {
                    var replyMessage = TemplateManager.GenerateActivity(CancelRouteResponses.CannotCancelActiveRoute);
                    await sc.Context.SendActivityAsync(replyMessage);
                }

                return await sc.EndDialogAsync();
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}