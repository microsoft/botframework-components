// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Solutions.Responses;
using PointOfInterestSkill.Models;
using PointOfInterestSkill.Responses.Shared;
using PointOfInterestSkill.Services;
using PointOfInterestSkill.Utilities;

namespace PointOfInterestSkill.Dialogs
{
    public class GetDirectionsDialog : PointOfInterestDialogBase
    {
        public GetDirectionsDialog(
            IServiceProvider serviceProvider)
            : base(nameof(GetDirectionsDialog), serviceProvider)
        {
            var checkCurrentLocation = new WaterfallStep[]
            {
                CheckForCurrentCoordinatesBeforeGetDirectionsAsync,
                ConfirmCurrentLocationAsync,
                ProcessCurrentLocationSelectionAsync,
                RouteToFindPointOfInterestDialogAsync
            };

            var findPointOfInterest = new WaterfallStep[]
            {
                GetPointOfInterestLocationsAsync,
                SendEventAsync,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.CheckForCurrentLocation, checkCurrentLocation));
            AddDialog(new WaterfallDialog(Actions.FindPointOfInterest, findPointOfInterest));

            // Set starting dialog for component
            InitialDialogId = Actions.CheckForCurrentLocation;
        }

        protected async Task<DialogTurnResult> CheckForCurrentCoordinatesBeforeGetDirectionsAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context, () => new PointOfInterestSkillState(), cancellationToken);
            var hasCurrentCoordinates = state.CheckForValidCurrentCoordinates();
            if (hasCurrentCoordinates || !string.IsNullOrEmpty(state.Address))
            {
                return await sc.ReplaceDialogAsync(Actions.FindPointOfInterest, cancellationToken: cancellationToken);
            }

            return await sc.PromptAsync(Actions.CurrentLocationPrompt, new PromptOptions { Prompt = TemplateManager.GenerateActivity(POISharedResponses.PromptForCurrentLocation) }, cancellationToken);
        }

        protected async Task<DialogTurnResult> RouteToFindPointOfInterestDialogAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            return await sc.ReplaceDialogAsync(Actions.FindPointOfInterest, cancellationToken: cancellationToken);
        }

        protected async Task<DialogTurnResult> SendEventAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            bool shouldInterrupt = sc.Context.TurnState.ContainsKey(StateProperties.InterruptKey);

            if (shouldInterrupt)
            {
                return await sc.CancelAllDialogsAsync(cancellationToken);
            }

            var state = await Accessor.GetAsync(sc.Context, () => new PointOfInterestSkillState(), cancellationToken);
            var userSelectIndex = 0;

            if (sc.Result is bool)
            {
                state.Destination = state.LastFoundPointOfInterests[userSelectIndex];
                state.LastFoundPointOfInterests = null;
            }
            else if (sc.Result is FoundChoice)
            {
                // Update the destination state with user choice.
                userSelectIndex = (sc.Result as FoundChoice).Index;

                if (userSelectIndex < 0 || userSelectIndex >= state.LastFoundPointOfInterests.Count)
                {
                    await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(POISharedResponses.CancellingMessage), cancellationToken);
                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }

                state.Destination = state.LastFoundPointOfInterests[userSelectIndex];
                state.LastFoundPointOfInterests = null;
            }

            if (SupportOpenDefaultAppReply(sc.Context))
            {
                await sc.Context.SendActivityAsync(CreateOpenDefaultAppReply(sc.Context.Activity, state.Destination, OpenDefaultAppType.Map), cancellationToken);
            }

            var response = state.IsAction ? ConvertToResponse(state.Destination) : null;

            return await sc.NextAsync(response, cancellationToken);
        }
    }
}
