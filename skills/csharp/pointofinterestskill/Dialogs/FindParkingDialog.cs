// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Extensions.DependencyInjection;
using PointOfInterestSkill.Models;
using PointOfInterestSkill.Responses.Shared;
using PointOfInterestSkill.Services;
using PointOfInterestSkill.Utilities;
using SkillServiceLibrary.Models;

namespace PointOfInterestSkill.Dialogs
{
    public class FindParkingDialog : PointOfInterestDialogBase
    {
        public FindParkingDialog(
            IServiceProvider serviceProvider)
            : base(nameof(FindParkingDialog), serviceProvider)
        {
            var checkCurrentLocation = new WaterfallStep[]
            {
                CheckForCurrentCoordinatesBeforeFindParkingAsync,
                ConfirmCurrentLocationAsync,
                ProcessCurrentLocationSelectionAsync,
                RouteToFindFindParkingDialogAsync,
            };

            var findParking = new WaterfallStep[]
            {
                GetParkingInterestPointsAsync,
                ProcessPointOfInterestSelectionAsync,
                ProcessPointOfInterestActionAsync,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.CheckForCurrentLocation, checkCurrentLocation));
            AddDialog(new WaterfallDialog(Actions.FindParking, findParking));
            AddDialog(serviceProvider.GetService<RouteDialog>());

            // Set starting dialog for component
            InitialDialogId = Actions.CheckForCurrentLocation;
        }

        /// <summary>
        /// Check for the current coordinates and if missing, prompt user.
        /// </summary>
        /// <param name="sc">Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        protected async Task<DialogTurnResult> CheckForCurrentCoordinatesBeforeFindParkingAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(sc.Context, () => new PointOfInterestSkillState(), cancellationToken);
            var hasCurrentCoordinates = state.CheckForValidCurrentCoordinates();
            if (hasCurrentCoordinates || !string.IsNullOrEmpty(state.Address))
            {
                return await sc.ReplaceDialogAsync(Actions.FindParking, cancellationToken: cancellationToken);
            }

            return await sc.PromptAsync(Actions.CurrentLocationPrompt, new PromptOptions { Prompt = TemplateManager.GenerateActivity(POISharedResponses.PromptForCurrentLocation) }, cancellationToken);
        }

        /// <summary>
        /// Replaces the active dialog with the FindParking waterfall dialog.
        /// </summary>
        /// <param name="sc">WaterfallStepContext.</param>
        /// <param name="cancellationToken">CancellationToken.</param>
        /// <returns>DialogTurnResult.</returns>
        protected async Task<DialogTurnResult> RouteToFindFindParkingDialogAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            return await sc.ReplaceDialogAsync(Actions.FindParking, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Look up parking points of interest, render cards, and ask user which to route to.
        /// </summary>
        /// <param name="sc">Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        protected async Task<DialogTurnResult> GetParkingInterestPointsAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, () => new PointOfInterestSkillState(), cancellationToken);

                var mapsService = ServiceManager.InitMapsService(Settings, sc.Context.Activity.Locale);
                var addressMapsService = ServiceManager.InitAddressMapsService(Settings, sc.Context.Activity.Locale);

                var pointOfInterestList = new List<PointOfInterestModel>();
                var cards = new List<Card>();

                if (!string.IsNullOrEmpty(state.Address))
                {
                    // Get first POI matched with address, if there are multiple this could be expanded to confirm which address to use
                    var pointOfInterestAddressList = await addressMapsService.GetPointOfInterestListByQueryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Address);

                    if (pointOfInterestAddressList.Any())
                    {
                        var pointOfInterest = pointOfInterestAddressList[0];

                        // TODO nearest here is not for state.CurrentCoordinates
                        pointOfInterestList = await mapsService.GetPointOfInterestListByParkingCategoryAsync(pointOfInterest.Geolocation.Latitude, pointOfInterest.Geolocation.Longitude, state.PoiType);
                        cards = await GetPointOfInterestLocationCardsAsync(sc, pointOfInterestList, mapsService, cancellationToken);
                    }
                    else
                    {
                        // Find parking lot near address
                        pointOfInterestList = await mapsService.GetPointOfInterestListByParkingCategoryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.PoiType);
                        cards = await GetPointOfInterestLocationCardsAsync(sc, pointOfInterestList, mapsService, cancellationToken);
                    }
                }
                else
                {
                    // No entities identified, find nearby parking lots
                    pointOfInterestList = await mapsService.GetPointOfInterestListByParkingCategoryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.PoiType);
                    cards = await GetPointOfInterestLocationCardsAsync(sc, pointOfInterestList, mapsService, cancellationToken);
                }

                if (cards.Count == 0)
                {
                    var replyMessage = TemplateManager.GenerateActivity(POISharedResponses.NoLocationsFound);
                    await sc.Context.SendActivityAsync(replyMessage, cancellationToken);
                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }
                else if (cards.Count == 1)
                {
                    // only to indicate it is only one result
                    return await sc.NextAsync(true, cancellationToken);
                }
                else
                {
                    var containerCard = await GetContainerCardAsync(sc.Context, CardNames.PointOfInterestOverviewContainer, state.CurrentCoordinates, pointOfInterestList, addressMapsService, cancellationToken);

                    var options = GetPointOfInterestPrompt(POISharedResponses.MultipleLocationsFound, containerCard, "Container", cards);

                    return await sc.PromptAsync(Actions.SelectPointOfInterestPrompt, options, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}
