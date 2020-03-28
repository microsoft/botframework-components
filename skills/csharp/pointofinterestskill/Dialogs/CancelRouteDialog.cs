// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Util;
using PointOfInterestSkill.Models;
using PointOfInterestSkill.Responses.CancelRoute;
using PointOfInterestSkill.Utilities;

namespace PointOfInterestSkill.Dialogs
{
    public class CancelRouteDialog : PointOfInterestDialogBase
    {
        public CancelRouteDialog(
            IServiceProvider serviceProvider)
            : base(nameof(CancelRouteDialog), serviceProvider)
        {
            var cancelRoute = new WaterfallStep[]
            {
                CancelActiveRouteAsync,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.CancelActiveRoute, cancelRoute));

            // Set starting dialog for component
            InitialDialogId = Actions.CancelActiveRoute;
        }

        public async Task<DialogTurnResult> CancelActiveRouteAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, () => new PointOfInterestSkillState(), cancellationToken);
                if (state.ActiveRoute != null)
                {
                    var replyMessage = TemplateManager.GenerateActivity(CancelRouteResponses.CancelActiveRoute);
                    await sc.Context.SendActivityAsync(replyMessage, cancellationToken);
                    state.ActiveRoute = null;
                    state.Destination = null;
                }
                else
                {
                    var replyMessage = TemplateManager.GenerateActivity(CancelRouteResponses.CannotCancelActiveRoute);
                    await sc.Context.SendActivityAsync(replyMessage, cancellationToken);
                }

                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}