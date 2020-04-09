// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using PointOfInterestSkill.Responses.Shared;
using PointOfInterestSkill.Models;
using PointOfInterestSkill.Utilities;

namespace PointOfInterestSkill.Dialogs
{
    public class FindPointOfInterestDialog : PointOfInterestDialogBase
    {
        public FindPointOfInterestDialog(
            IServiceProvider serviceProvider)
            : base(nameof(FindPointOfInterestDialog), serviceProvider)
        {
            var checkCurrentLocation = new WaterfallStep[]
            {
                CheckForCurrentCoordinatesBeforeFindPointOfInterestAsync,
                ConfirmCurrentLocationAsync,
                ProcessCurrentLocationSelectionAsync,
                RouteToFindPointOfInterestDialogAsync,
            };

            var findPointOfInterest = new WaterfallStep[]
            {
                GetPointOfInterestLocationsAsync,
                ProcessPointOfInterestSelectionAsync,
                ProcessPointOfInterestActionAsync,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.CheckForCurrentLocation, checkCurrentLocation));
            AddDialog(new WaterfallDialog(Actions.FindPointOfInterest, findPointOfInterest));
            AddDialog(serviceProvider.GetService<RouteDialog>());

            // Set starting dialog for component
            InitialDialogId = Actions.CheckForCurrentLocation;
            GoBackDialogId = Actions.FindPointOfInterest;
        }

        /// <summary>
        /// Check for the current coordinates and if missing, prompt user.
        /// </summary>
        /// <param name="sc">Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        protected async Task<DialogTurnResult> CheckForCurrentCoordinatesBeforeFindPointOfInterestAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context, () => new PointOfInterestSkillState(), cancellationToken);
            var hasCurrentCoordinates = state.CheckForValidCurrentCoordinates();
            if (hasCurrentCoordinates || !string.IsNullOrEmpty(state.Address))
            {
                return await sc.ReplaceDialogAsync(Actions.FindPointOfInterest, cancellationToken: cancellationToken);
            }

            return await sc.PromptAsync(Actions.CurrentLocationPrompt, new PromptOptions { Prompt = TemplateManager.GenerateActivity(POISharedResponses.PromptForCurrentLocation) }, cancellationToken);
        }

        /// <summary>
        /// Replaces the active dialog with the FindPointOfInterest waterfall dialog.
        /// </summary>
        /// <param name="sc">WaterfallStepContext.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <returns>DialogTurnResult.</returns>
        protected async Task<DialogTurnResult> RouteToFindPointOfInterestDialogAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            return await sc.ReplaceDialogAsync(Actions.FindPointOfInterest, cancellationToken: cancellationToken);
        }
    }
}