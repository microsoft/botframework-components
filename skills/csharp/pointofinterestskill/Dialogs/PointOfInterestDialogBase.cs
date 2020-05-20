// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Models;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Extensions.DependencyInjection;
using PointOfInterestSkill.Models;
using PointOfInterestSkill.Responses.FindPointOfInterest;
using PointOfInterestSkill.Responses.Shared;
using PointOfInterestSkill.Services;
using PointOfInterestSkill.Utilities;
using SkillServiceLibrary.Models;
using SkillServiceLibrary.Services;
using SkillServiceLibrary.Utilities;
using static Microsoft.Recognizers.Text.Culture;

namespace PointOfInterestSkill.Dialogs
{
    public class PointOfInterestDialogBase : ComponentDialog
    {
        private const string FallbackPointOfInterestImageFileName = "default_pointofinterest.png";

        // Constants
        // TODO same as the one in ConfirmPrompt
        private static readonly Dictionary<string, string> ChoiceDefaults = new Dictionary<string, string>()
        {
            { Spanish, "Sí" },
            { Dutch, "Ja" },
            { English, "Yes" },
            { French, "Oui" },
            { German, "Ja" },
            { Japanese, "はい" },
            { Portuguese, "Sim" },
            { Chinese, "是的" },
        };

        private readonly IHttpContextAccessor _httpContext;

        public PointOfInterestDialogBase(
            string dialogId,
            IServiceProvider serviceProvider)
            : base(dialogId)
        {
            Settings = serviceProvider.GetService<BotSettings>();
            Services = serviceProvider.GetService<BotServices>();
            TemplateManager = serviceProvider.GetService<LocaleTemplateManager>();
            var conversationState = serviceProvider.GetService<ConversationState>();
            Accessor = conversationState.CreateProperty<PointOfInterestSkillState>(nameof(PointOfInterestSkillState));
            ServiceManager = serviceProvider.GetService<IServiceManager>();
            _httpContext = serviceProvider.GetService<IHttpContextAccessor>();

            AddDialog(new TextPrompt(Actions.CurrentLocationPrompt));
            AddDialog(new ConfirmPrompt(Actions.ConfirmPrompt) { Style = ListStyle.Auto, });
            AddDialog(new ChoicePrompt(Actions.SelectPointOfInterestPrompt, CanNoInterruptablePromptValidatorAsync) { Style = ListStyle.None });
            AddDialog(new ChoicePrompt(Actions.SelectActionPrompt, CanBackInterruptablePromptValidatorAsync) { Style = ListStyle.None });
            AddDialog(new ChoicePrompt(Actions.SelectRoutePrompt) { ChoiceOptions = new ChoiceFactoryOptions { InlineSeparator = string.Empty, InlineOr = string.Empty, InlineOrMore = string.Empty, IncludeNumbers = true } });
        }

        public enum OpenDefaultAppType
        {
            /// <summary>
            /// Telephone app type.
            /// </summary>
            Telephone,

            /// <summary>
            /// Map app type.
            /// </summary>
            Map,
        }

        protected BotSettings Settings { get; }

        protected BotServices Services { get; }

        protected IStatePropertyAccessor<PointOfInterestSkillState> Accessor { get; }

        protected IServiceManager ServiceManager { get; }

        protected LocaleTemplateManager TemplateManager { get; }

        protected string GoBackDialogId { get; set; }

        public static Activity CreateOpenDefaultAppReply(Activity activity, PointOfInterestModel destination, OpenDefaultAppType type)
        {
            var replyEvent = activity.CreateReply();
            replyEvent.Type = ActivityTypes.Event;
            replyEvent.Name = "OpenDefaultApp";

            var value = new OpenDefaultApp();
            switch (type)
            {
                case OpenDefaultAppType.Map: value.MapsUri = $"geo:{destination.Geolocation.Latitude},{destination.Geolocation.Longitude}"; break;
                case OpenDefaultAppType.Telephone: value.TelephoneUri = "tel:" + destination.Phone; break;
            }

            replyEvent.Value = value;
            return replyEvent;
        }

        /// <summary>
        /// Looks up the current location and prompts user to select one.
        /// </summary>
        /// <param name="sc">Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        protected async Task<DialogTurnResult> ConfirmCurrentLocationAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, () => new PointOfInterestSkillState(), cancellationToken);
                var service = ServiceManager.InitAddressMapsService(Settings);

                var pointOfInterestList = await service.GetPointOfInterestListByAddressAsync(double.NaN, double.NaN, sc.Result.ToString());
                var cards = await GetPointOfInterestLocationCardsAsync(sc, pointOfInterestList, service, cancellationToken);

                if (cards.Count() == 0)
                {
                    var replyMessage = TemplateManager.GenerateActivity(POISharedResponses.NoLocationsFound);
                    await sc.Context.SendActivityAsync(replyMessage, cancellationToken);

                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }
                else
                {
                    var containerCard = await GetContainerCardAsync(sc.Context, CardNames.PointOfInterestOverviewContainer, state.CurrentCoordinates, pointOfInterestList, service, cancellationToken);

                    var options = GetPointOfInterestPrompt(cards.Count == 1 ? POISharedResponses.CurrentLocationSingleSelection : POISharedResponses.CurrentLocationMultipleSelection, containerCard, "Container", cards);

                    if (cards.Count == 1)
                    {
                        // Workaround. In teams, HeroCard will be used for prompt and adaptive card could not be shown. So send them separately
                        if (Channel.GetChannelId(sc.Context) == Channels.Msteams)
                        {
                            await sc.Context.SendActivityAsync(options.Prompt, cancellationToken);
                            options.Prompt = null;
                        }
                    }

                    return await sc.PromptAsync(cards.Count == 1 ? Actions.ConfirmPrompt : Actions.SelectPointOfInterestPrompt, options, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        /// <summary>
        /// Process result from choice prompt to select current location.
        /// </summary>
        /// <param name="sc">Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        protected async Task<DialogTurnResult> ProcessCurrentLocationSelectionAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, () => new PointOfInterestSkillState(), cancellationToken);
                bool shouldInterrupt = sc.Context.TurnState.ContainsKey(StateProperties.InterruptKey);

                if (shouldInterrupt)
                {
                    return await sc.CancelAllDialogsAsync(cancellationToken);
                }

                var cancelMessage = TemplateManager.GenerateActivity(POISharedResponses.CancellingMessage);

                if (sc.Result != null)
                {
                    var userSelectIndex = 0;

                    if (sc.Result is bool)
                    {
                        // If true, update the current coordinates state. If false, end dialog.
                        if ((bool)sc.Result)
                        {
                            state.CurrentCoordinates = state.LastFoundPointOfInterests[userSelectIndex].Geolocation;
                            state.LastFoundPointOfInterests = null;
                        }
                        else
                        {
                            return await sc.ReplaceDialogAsync(Actions.CheckForCurrentLocation, cancellationToken: cancellationToken);
                        }
                    }
                    else if (sc.Result is FoundChoice)
                    {
                        // Update the current coordinates state with user choice.
                        userSelectIndex = (sc.Result as FoundChoice).Index;

                        if (userSelectIndex == SpecialChoices.Cancel || userSelectIndex >= state.LastFoundPointOfInterests.Count)
                        {
                            return await sc.ReplaceDialogAsync(Actions.CheckForCurrentLocation, cancellationToken: cancellationToken);
                        }

                        state.CurrentCoordinates = state.LastFoundPointOfInterests[userSelectIndex].Geolocation;
                        state.LastFoundPointOfInterests = null;
                    }

                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }

                await sc.Context.SendActivityAsync(cancelMessage, cancellationToken);

                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        /// <summary>
        /// Look up points of interest, render cards, and ask user which to route to.
        /// </summary>
        /// <param name="sc">Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        protected async Task<DialogTurnResult> GetPointOfInterestLocationsAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, () => new PointOfInterestSkillState(), cancellationToken);

                var service = ServiceManager.InitMapsService(Settings, sc.Context.Activity.Locale);
                var addressMapsService = ServiceManager.InitAddressMapsService(Settings, sc.Context.Activity.Locale);

                var pointOfInterestList = new List<PointOfInterestModel>();
                var cards = new List<Card>();

                if (!string.IsNullOrEmpty(state.Category))
                {
                    if (!string.IsNullOrEmpty(state.Keyword))
                    {
                        throw new Exception("Should search only category or keyword!");
                    }

                    if (string.IsNullOrEmpty(state.Address))
                    {
                        // Fuzzy query search with keyword
                        pointOfInterestList = await service.GetPointOfInterestListByCategoryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Category, state.PoiType, true);
                        cards = await GetPointOfInterestLocationCardsAsync(sc, pointOfInterestList, service, cancellationToken);
                    }
                    else
                    {
                        // Get first POI matched with address, if there are multiple this could be expanded to confirm which address to use
                        var pointOfInterestAddressList = await addressMapsService.GetPointOfInterestListByAddressAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Address, state.PoiType);

                        if (pointOfInterestAddressList.Any())
                        {
                            var pointOfInterest = pointOfInterestAddressList[0];

                            // TODO nearest here is not for current
                            pointOfInterestList = await service.GetPointOfInterestListByCategoryAsync(pointOfInterest.Geolocation.Latitude, pointOfInterest.Geolocation.Longitude, state.Category, state.PoiType, true);
                            cards = await GetPointOfInterestLocationCardsAsync(sc, pointOfInterestList, service, cancellationToken);
                        }
                        else
                        {
                            // No POIs found from address - search near current coordinates
                            pointOfInterestList = await service.GetPointOfInterestListByCategoryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Category, state.PoiType, true);
                            cards = await GetPointOfInterestLocationCardsAsync(sc, pointOfInterestList, service, cancellationToken);
                        }
                    }
                }
                else if (string.IsNullOrEmpty(state.Keyword) && string.IsNullOrEmpty(state.Address))
                {
                    // No entities identified, find nearby locations
                    pointOfInterestList = await service.GetNearbyPointOfInterestListAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.PoiType);
                    cards = await GetPointOfInterestLocationCardsAsync(sc, pointOfInterestList, service, cancellationToken);
                }
                else if (!string.IsNullOrEmpty(state.Keyword) && !string.IsNullOrEmpty(state.Address))
                {
                    // Get first POI matched with address, if there are multiple this could be expanded to confirm which address to use
                    var pointOfInterestAddressList = await addressMapsService.GetPointOfInterestListByAddressAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Address, state.PoiType);

                    if (pointOfInterestAddressList.Any())
                    {
                        var pointOfInterest = pointOfInterestAddressList[0];

                        // TODO nearest here is not for current
                        pointOfInterestList = await service.GetPointOfInterestListByQueryAsync(pointOfInterest.Geolocation.Latitude, pointOfInterest.Geolocation.Longitude, state.Keyword, state.PoiType);
                        cards = await GetPointOfInterestLocationCardsAsync(sc, pointOfInterestList, service, cancellationToken);
                    }
                    else
                    {
                        // No POIs found from address - search near current coordinates
                        pointOfInterestList = await service.GetPointOfInterestListByQueryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Keyword, state.PoiType);
                        cards = await GetPointOfInterestLocationCardsAsync(sc, pointOfInterestList, service, cancellationToken);
                    }
                }
                else if (!string.IsNullOrEmpty(state.Keyword))
                {
                    // Fuzzy query search with keyword
                    pointOfInterestList = await service.GetPointOfInterestListByQueryAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Keyword, state.PoiType);
                    cards = await GetPointOfInterestLocationCardsAsync(sc, pointOfInterestList, service, cancellationToken);
                }
                else if (!string.IsNullOrEmpty(state.Address))
                {
                    // Fuzzy query search with address
                    pointOfInterestList = await addressMapsService.GetPointOfInterestListByAddressAsync(state.CurrentCoordinates.Latitude, state.CurrentCoordinates.Longitude, state.Address, state.PoiType);
                    cards = await GetPointOfInterestLocationCardsAsync(sc, pointOfInterestList, addressMapsService, cancellationToken);
                }

                if (cards.Count() == 0)
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

                    List<PointOfInterestModelSlim> slimPointOfInterestList = pointOfInterestList.Select(x => new PointOfInterestModelSlim(x)).ToList();

                    var comcastResponseData = new ComcastPointOfInterestResponse() { Results = slimPointOfInterestList, Municipality = state.Municipality };

                    options.Prompt.ChannelData = comcastResponseData;

                    return await sc.PromptAsync(Actions.SelectPointOfInterestPrompt, options, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        /// <summary>
        /// Process result from choice prompt and begin route direction dialog.
        /// </summary>
        /// <param name="sc">Step Context.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <returns>Dialog Turn Result.</returns>
        protected async Task<DialogTurnResult> ProcessPointOfInterestSelectionAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, () => new PointOfInterestSkillState(), cancellationToken);
                bool shouldInterrupt = sc.Context.TurnState.ContainsKey(StateProperties.InterruptKey);

                if (shouldInterrupt)
                {
                    return await sc.CancelAllDialogsAsync(cancellationToken);
                }

                var defaultReplyMessage = TemplateManager.GenerateActivity(POISharedResponses.GetRouteToActiveLocationLater);

                if (sc.Result != null)
                {
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

                        if (userSelectIndex == SpecialChoices.Cancel || userSelectIndex >= state.LastFoundPointOfInterests.Count)
                        {
                            await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(POISharedResponses.CancellingMessage), cancellationToken);
                            return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                        }

                        state.Destination = state.LastFoundPointOfInterests[userSelectIndex];
                        state.LastFoundPointOfInterests = null;
                    }

                    var options = new PromptOptions()
                    {
                        Choices = new List<Choice>()
                    };

                    var callString = TemplateManager.GetString(PointOfInterestSharedStrings.CALL);
                    var showDirectionsString = TemplateManager.GetString(PointOfInterestSharedStrings.SHOW_DIRECTIONS);
                    var startNavigationString = TemplateManager.GetString(PointOfInterestSharedStrings.START_NAVIGATION);
                    var cardTitleString = TemplateManager.GetString(PointOfInterestSharedStrings.CARD_TITLE);

                    bool hasCall = !string.IsNullOrEmpty(state.Destination.Phone);
                    if (hasCall)
                    {
                        options.Choices.Add(new Choice { Value = callString });
                    }

                    options.Choices.Add(new Choice { Value = showDirectionsString });
                    options.Choices.Add(new Choice { Value = startNavigationString });

                    var mapsService = ServiceManager.InitMapsService(Settings, sc.Context.Activity.Locale);
                    state.Destination = await mapsService.GetPointOfInterestDetailsAsync(state.Destination, ImageSize.DetailsWidth, ImageSize.DetailsHeight);

                    state.Destination.ProviderDisplayText = state.Destination.GenerateProviderDisplayText();

                    state.Destination.CardTitle = cardTitleString;
                    state.Destination.ActionCall = callString;
                    state.Destination.ActionShowDirections = showDirectionsString;
                    state.Destination.ActionStartNavigation = startNavigationString;

                    var card = new Card
                    {
                        Name = GetDivergedCardName(sc.Context, hasCall ? CardNames.PointOfInterestDetails : CardNames.PointOfInterestDetailsNoCall),
                        Data = state.Destination,
                    };

                    string promptResponse = hasCall ? FindPointOfInterestResponses.PointOfInterestDetails : FindPointOfInterestResponses.PointOfInterestDetailsNoCall;

                    if (promptResponse == null)
                    {
                        options.Prompt = TemplateManager.GenerateActivity(card);
                    }
                    else
                    {
                        options.Prompt = TemplateManager.GenerateActivity(promptResponse, card, card.Data);
                    }

                    // If DestinationActionType is provided, skip the SelectActionPrompt with appropriate choice index
                    if (state.DestinationActionType != DestinationActionType.None)
                    {
                        int choiceIndex = -1;
                        if (state.DestinationActionType == DestinationActionType.Call)
                        {
                            choiceIndex = hasCall ? 0 : -1;
                        }
                        else if (state.DestinationActionType == DestinationActionType.ShowDirectionsThenStartNavigation)
                        {
                            choiceIndex = hasCall ? 1 : 0;
                        }
                        else if (state.DestinationActionType == DestinationActionType.StartNavigation)
                        {
                            choiceIndex = hasCall ? 2 : 1;
                        }

                        if (choiceIndex >= 0)
                        {
                            await sc.Context.SendActivityAsync(options.Prompt, cancellationToken);
                            return await sc.NextAsync(new FoundChoice() { Index = choiceIndex }, cancellationToken);
                        }
                    }

                    return await sc.PromptAsync(Actions.SelectActionPrompt, options, cancellationToken);
                }

                await sc.Context.SendActivityAsync(defaultReplyMessage, cancellationToken);

                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        protected async Task<DialogTurnResult> ProcessPointOfInterestActionAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context, () => new PointOfInterestSkillState(), cancellationToken);
            bool shouldInterrupt = sc.Context.TurnState.ContainsKey(StateProperties.InterruptKey);

            if (shouldInterrupt)
            {
                return await sc.CancelAllDialogsAsync(cancellationToken);
            }

            var choice = sc.Result as FoundChoice;
            int choiceIndex = choice.Index;

            if (choiceIndex == SpecialChoices.GoBack)
            {
                return await sc.ReplaceDialogAsync(GoBackDialogId, null, cancellationToken);
            }

            SingleDestinationResponse response = null;

            // TODO skip call button
            if (string.IsNullOrEmpty(state.Destination.Phone))
            {
                choiceIndex += 1;
            }

            if (choiceIndex == 0)
            {
                if (SupportOpenDefaultAppReply(sc.Context))
                {
                    await sc.Context.SendActivityAsync(CreateOpenDefaultAppReply(sc.Context.Activity, state.Destination, OpenDefaultAppType.Telephone), cancellationToken);
                }

                response = state.IsAction ? ConvertToResponse(state.Destination) : null;
            }
            else if (choiceIndex == 1)
            {
                return await sc.ReplaceDialogAsync(nameof(RouteDialog), cancellationToken: cancellationToken);
            }
            else if (choiceIndex == 2)
            {
                if (SupportOpenDefaultAppReply(sc.Context))
                {
                    await sc.Context.SendActivityAsync(CreateOpenDefaultAppReply(sc.Context.Activity, state.Destination, OpenDefaultAppType.Map), cancellationToken);
                }

                response = state.IsAction ? ConvertToResponse(state.Destination) : null;
            }

            return await sc.NextAsync(response, cancellationToken);
        }

        protected async Task<Card> GetContainerCardAsync(ITurnContext context, string name, LatLng currentCoordinates, List<PointOfInterestModel> pointOfInterestList, IGeoSpatialService service, CancellationToken cancellationToken)
        {
            var model = new PointOfInterestModel
            {
                CardTitle = TemplateManager.GetString(PointOfInterestSharedStrings.CARD_TITLE),
                PointOfInterestImageUrl = await service.GetAllPointOfInterestsImageAsync(currentCoordinates, pointOfInterestList, ImageSize.OverviewWidth, ImageSize.OverviewHeight),
                Provider = new SortedSet<string> { service.Provider }
            };

            foreach (var poi in pointOfInterestList)
            {
                model.Provider.UnionWith(poi.Provider);
            }

            model.ProviderDisplayText = model.GenerateProviderDisplayText();

            return new Card
            {
                Name = GetDivergedCardName(context, name),
                Data = model
            };
        }

        /// <summary>
        /// Gets ChoicePrompt options with a formatted display name if there are identical locations.
        /// Handle the special yes no case when cards has only one.
        /// </summary>
        /// <param name="prompt">Prompt string.</param>
        /// <param name="containerCard">Container card.</param>
        /// <param name="container">Container.</param>
        /// <param name="cards">List of Cards. Data must be PointOfInterestModel.</param>
        /// <returns>PromptOptions.</returns>
        protected PromptOptions GetPointOfInterestPrompt(string prompt, Card containerCard, string container, List<Card> cards)
        {
            var pointOfInterestList = cards.Select(card => card.Data as PointOfInterestModel).ToList();

            var options = new PromptOptions()
            {
                Choices = new List<Choice>(),
            };

            for (var i = 0; i < pointOfInterestList.Count; ++i)
            {
                var address = pointOfInterestList[i].Address;

                var synonyms = new List<string>()
                {
                    address,
                };

                var choice = new Choice()
                {
                    // Use speak first for SpeechUtility.ListToSpeechReadyString
                    Value = pointOfInterestList[i].Speak,
                    Synonyms = synonyms,
                };
                options.Choices.Add(choice);

                pointOfInterestList[i].SubmitText = pointOfInterestList[i].RawSpeak;
            }

            if (cards.Count == 1)
            {
                pointOfInterestList[0].SubmitText = GetConfirmPromptTrue();
            }

            options.Prompt = new Activity();

            var data = new
            {
                Count = options.Choices.Count,
                Options = SpeechUtility.ListToSpeechReadyString(options),
            };

            options.Prompt = TemplateManager.GenerateActivity(prompt, cards);

            // Restore Value to SubmitText
            for (var i = 0; i < pointOfInterestList.Count; ++i)
            {
                options.Choices[i].Value = pointOfInterestList[i].RawSpeak;
            }

            return options;
        }

        // service: for details. the one generates pointOfInterestList
        protected async Task<List<Card>> GetPointOfInterestLocationCardsAsync(DialogContext sc, List<PointOfInterestModel> pointOfInterestList, IGeoSpatialService service, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context, () => new PointOfInterestSkillState(), cancellationToken);
            var cards = new List<Card>();

            if (pointOfInterestList != null && pointOfInterestList.Count > 0)
            {
                for (var i = 0; i < pointOfInterestList.Count; i++)
                {
                    pointOfInterestList[i] = await service.GetPointOfInterestDetailsAsync(pointOfInterestList[i], ImageSize.OverviewItemWidth, ImageSize.OverviewItemHeight);

                    // Increase by one to avoid zero based options to the user which are confusing
                    pointOfInterestList[i].Index = i + 1;

                    if (string.IsNullOrEmpty(pointOfInterestList[i].PointOfInterestImageUrl))
                    {
                        pointOfInterestList[i].PointOfInterestImageUrl = GetCardImageUri(FallbackPointOfInterestImageFileName);
                    }

                    if (string.IsNullOrEmpty(pointOfInterestList[i].Name))
                    {
                        // Show address as the name
                        pointOfInterestList[i].Name = pointOfInterestList[i].Address;
                        pointOfInterestList[i].Address = pointOfInterestList[i].AddressAlternative;
                    }
                }

                // Loop again as name may have changed
                for (var i = 0; i < pointOfInterestList.Count; i++)
                {
                    // If multiple points of interest share the same name, use their combined name & address as the speak property.
                    // Otherwise, just use the name.
                    if (pointOfInterestList.Where(x => x.Name == pointOfInterestList[i].Name).Skip(1).Any())
                    {
                        var promptTemplate = POISharedResponses.PointOfInterestSuggestedActionName;
                        var promptReplacements = new Dictionary<string, object>
                        {
                            { "Name", WebUtility.HtmlEncode(pointOfInterestList[i].Name) },
                            { "Address", $"<say-as interpret-as='address'>{WebUtility.HtmlEncode(pointOfInterestList[i].AddressForSpeak)}</say-as>" },
                        };
                        pointOfInterestList[i].Speak = TemplateManager.GenerateActivity(promptTemplate, promptReplacements).Speak;

                        promptReplacements = new Dictionary<string, object>
                        {
                            { "Name", pointOfInterestList[i].Name },
                            { "Address", pointOfInterestList[i].AddressForSpeak },
                        };
                        pointOfInterestList[i].RawSpeak = TemplateManager.GenerateActivity(promptTemplate, promptReplacements).Speak;
                    }
                    else
                    {
                        pointOfInterestList[i].Speak = WebUtility.HtmlEncode(pointOfInterestList[i].Name);
                        pointOfInterestList[i].RawSpeak = pointOfInterestList[i].Name;
                    }
                }

                state.LastFoundPointOfInterests = pointOfInterestList;

                foreach (var pointOfInterest in pointOfInterestList)
                {
                    cards.Add(new Card(GetDivergedCardName(sc.Context, CardNames.PointOfInterestOverviewDetails), pointOfInterest));
                }
            }

            return cards;
        }

        protected string GetFormattedTravelTimeSpanString(TimeSpan timeSpan)
        {
            var timeString = new StringBuilder();
            if (timeSpan.Hours == 1)
            {
                timeString.Append(timeSpan.Hours + $" {TemplateManager.GetString(PointOfInterestSharedStrings.HOUR)}");
            }
            else if (timeSpan.Hours > 1)
            {
                timeString.Append(timeSpan.Hours + $" {TemplateManager.GetString(PointOfInterestSharedStrings.HOURS)}");
            }

            if (timeString.Length != 0)
            {
                timeString.Append(" and ");
            }

            if (timeSpan.Minutes < 1)
            {
                timeString.Append($" {TemplateManager.GetString(PointOfInterestSharedStrings.LESS_THAN_A_MINUTE)}");
            }
            else if (timeSpan.Minutes == 1)
            {
                timeString.Append(timeSpan.Minutes + $" {TemplateManager.GetString(PointOfInterestSharedStrings.MINUTE)}");
            }
            else if (timeSpan.Minutes > 1)
            {
                timeString.Append(timeSpan.Minutes + $" {TemplateManager.GetString(PointOfInterestSharedStrings.MINUTES)}");
            }

            return timeString.ToString();
        }

        protected string GetFormattedTrafficDelayString(TimeSpan timeSpan)
        {
            var timeString = new StringBuilder();
            if (timeSpan.Hours == 1)
            {
                timeString.Append(timeSpan.Hours + $" {TemplateManager.GetString(PointOfInterestSharedStrings.HOUR)}");
            }
            else if (timeSpan.Hours > 1)
            {
                timeString.Append(timeSpan.Hours + $" {TemplateManager.GetString(PointOfInterestSharedStrings.HOURS)}");
            }

            if (timeString.Length != 0)
            {
                timeString.Append(" and ");
            }

            if (timeSpan.Minutes < 1)
            {
                timeString.Append($"{TemplateManager.GetString(PointOfInterestSharedStrings.LESS_THAN_A_MINUTE)}");
            }
            else if (timeSpan.Minutes == 1)
            {
                timeString.Append(timeSpan.Minutes + $" {TemplateManager.GetString(PointOfInterestSharedStrings.MINUTE)}");
            }
            else if (timeSpan.Minutes > 1)
            {
                timeString.Append(timeSpan.Minutes + $" {TemplateManager.GetString(PointOfInterestSharedStrings.MINUTES)}");
            }

            var timeReplacements = new Dictionary<string, object>
            {
                { "Time", timeString.ToString() }
            };

            if (timeString.Length != 0)
            {
                var timeTemplate = POISharedResponses.TrafficDelay;

                return TemplateManager.GenerateActivity(timeTemplate, timeReplacements).Text;
            }
            else
            {
                var timeTemplate = POISharedResponses.NoTrafficDelay;

                return TemplateManager.GenerateActivity(timeTemplate, timeReplacements).Text;
            }
        }

        protected string GetShortTravelTimespanString(TimeSpan timeSpan)
        {
            var timeString = new StringBuilder();
            if (timeSpan.Hours != 0)
            {
                timeString.Append(timeSpan.Hours + $" {TemplateManager.GetString(PointOfInterestSharedStrings.HOUR_ABBREVIATION)}");
            }

            if (timeSpan.Minutes < 1)
            {
                timeString.Append($"< 1 {TemplateManager.GetString(PointOfInterestSharedStrings.MINUTE_ABBREVIATION)}");
            }
            else
            {
                timeString.Append(timeSpan.Minutes + $" {TemplateManager.GetString(PointOfInterestSharedStrings.MINUTE_ABBREVIATION)}");
            }

            return timeString.ToString();
        }

        protected async Task<List<Card>> GetRouteDirectionsViewCardsAsync(DialogContext sc, RouteDirections routeDirections, IGeoSpatialService service, CancellationToken cancellationToken)
        {
            var routes = routeDirections.Routes;
            var state = await Accessor.GetAsync(sc.Context, () => new PointOfInterestSkillState(), cancellationToken);
            var cardData = new List<RouteDirectionsModel>();
            var cards = new List<Card>();
            var routeId = 0;

            if (routes != null)
            {
                state.FoundRoutes = routes.Select(route => route.Summary).ToList();

                var destination = state.Destination;
                destination.Provider.Add(routeDirections.Provider);

                foreach (var route in routes)
                {
                    var travelTimeSpan = TimeSpan.FromSeconds(route.Summary.TravelTimeInSeconds);
                    var trafficTimeSpan = TimeSpan.FromSeconds(route.Summary.TrafficDelayInSeconds);

                    // Set card data with formatted time strings and distance converted to miles
                    var routeDirectionsModel = new RouteDirectionsModel()
                    {
                        Name = destination.Name,
                        Address = destination.Address,
                        AvailableDetails = destination.AvailableDetails,
                        Hours = destination.Hours,
                        PointOfInterestImageUrl = await service.GetRouteImageAsync(destination, route, ImageSize.RouteWidth, ImageSize.RouteHeight),
                        TravelTime = GetShortTravelTimespanString(travelTimeSpan),
                        DelayStatus = GetFormattedTrafficDelayString(trafficTimeSpan),
                        Distance = $"{(route.Summary.LengthInMeters / 1609.344).ToString("N1")} {TemplateManager.GetString(PointOfInterestSharedStrings.MILES_ABBREVIATION)}",
                        ETA = route.Summary.ArrivalTime.ToShortTimeString(),
                        TravelTimeSpeak = GetFormattedTravelTimeSpanString(travelTimeSpan),
                        TravelDelaySpeak = GetFormattedTrafficDelayString(trafficTimeSpan),
                        ProviderDisplayText = destination.GenerateProviderDisplayText(),
                        Speak = GetFormattedTravelTimeSpanString(travelTimeSpan),
                        ActionStartNavigation = TemplateManager.GetString(PointOfInterestSharedStrings.START_NAVIGATION),
                        CardTitle = TemplateManager.GetString(PointOfInterestSharedStrings.CARD_TITLE)
                    };

                    cardData.Add(routeDirectionsModel);
                    routeId++;
                }

                foreach (var data in cardData)
                {
                    cards.Add(new Card(GetDivergedCardName(sc.Context, CardNames.PointOfInterestDetailsWithRoute), data));
                }
            }

            return cards;
        }

        // This method is called by any waterfall step that throws an exception to ensure consistency
        protected async Task HandleDialogExceptionsAsync(WaterfallStepContext sc, Exception ex, CancellationToken cancellationToken)
        {
            // send trace back to emulator
            var trace = new Activity(type: ActivityTypes.Trace, text: $"DialogException: {ex.Message}, StackTrace: {ex.StackTrace}");
            await sc.Context.SendActivityAsync(trace, cancellationToken);

            // log exception
            TelemetryClient.TrackException(ex, new Dictionary<string, string> { { nameof(sc.ActiveDialog), sc.ActiveDialog?.Id } });

            // send error message to bot user
            await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(POISharedResponses.PointOfInterestErrorMessage), cancellationToken);

            // clear state
            var state = await Accessor.GetAsync(sc.Context, () => new PointOfInterestSkillState(), cancellationToken);
            state.Clear();
            await sc.CancelAllDialogsAsync(cancellationToken);

            return;
        }

        protected async Task HandleDialogExceptionAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await Accessor.GetAsync(sc.Context, () => new PointOfInterestSkillState(), cancellationToken);
            state.Clear();
            await Accessor.SetAsync(sc.Context, state);
            await sc.CancelAllDialogsAsync(cancellationToken);
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

        protected string GetConfirmPromptTrue()
        {
            var culture = CultureInfo.CurrentUICulture.Name.ToLower();
            if (!ChoiceDefaults.ContainsKey(culture))
            {
                culture = English;
            }

            return ChoiceDefaults[culture];
        }

        // workaround. if connect skill directly to teams, the following response does not work.
        protected bool SupportOpenDefaultAppReply(ITurnContext turnContext)
        {
            return turnContext.IsSkill() || Channel.GetChannelId(turnContext) != Channels.Msteams;
        }

        protected SingleDestinationResponse ConvertToResponse(PointOfInterestModel model)
        {
            var response = new SingleDestinationResponse();
            response.ActionSuccess = true;
            response.Name = model.Name;
            response.Latitude = model.Geolocation.Latitude;
            response.Longitude = model.Geolocation.Longitude;
            response.Telephone = model.Phone;
            response.Address = model.Address;
            return response;
        }

        private string GetCardImageUri(string imagePath)
        {
            var serverUrl = _httpContext.HttpContext.Request.Scheme + "://" + _httpContext.HttpContext.Request.Host.Value;
            return $"{serverUrl}/images/{imagePath}";
        }

        private async Task<bool> InterruptablePromptValidatorAsync<T>(PromptValidatorContext<T> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
            {
                return true;
            }
            else
            {
                var state = await Accessor.GetAsync(promptContext.Context, () => new PointOfInterestSkillState(), cancellationToken);
                if (state.IsAction)
                {
                    return false;
                }

                var poiResult = promptContext.Context.TurnState.Get<PointOfInterestLuis>(StateProperties.POILuisResultKey);
                if (poiResult == null)
                {
                    return false;
                }

                var topIntent = poiResult.TopIntent();

                if (topIntent.score > 0.5 && topIntent.intent != PointOfInterestLuis.Intent.None)
                {
                    promptContext.Context.TurnState.Add(StateProperties.InterruptKey, new object());
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private async Task<bool> CanNoInterruptablePromptValidatorAsync(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
            {
                return true;
            }
            else
            {
                var generalLuisResult = promptContext.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                if (generalLuisResult == null)
                {
                    return false;
                }

                var intent = generalLuisResult.TopIntent().intent;
                if (intent == General.Intent.Reject || intent == General.Intent.SelectNone)
                {
                    promptContext.Recognized.Value = new FoundChoice { Index = SpecialChoices.Cancel };
                    return true;
                }
                else
                {
                    return await InterruptablePromptValidatorAsync(promptContext, cancellationToken);
                }
            }
        }

        private async Task<bool> CanBackInterruptablePromptValidatorAsync(PromptValidatorContext<FoundChoice> promptContext, CancellationToken cancellationToken)
        {
            if (promptContext.Recognized.Succeeded)
            {
                return true;
            }
            else
            {
                var generalLuisResult = promptContext.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                if (generalLuisResult == null)
                {
                    return false;
                }

                var intent = generalLuisResult.TopIntent().intent;
                if (intent == General.Intent.GoBack)
                {
                    promptContext.Recognized.Value = new FoundChoice { Index = SpecialChoices.GoBack };
                    return true;
                }
                else
                {
                    return await InterruptablePromptValidatorAsync(promptContext, cancellationToken);
                }
            }
        }

        protected static class SpecialChoices
        {
            public const int Cancel = -1;
            public const int GoBack = -2;
        }

        private static class ImageSize
        {
            public const int RouteWidth = 440;
            public const int RouteHeight = 240;
            public const int OverviewWidth = 440;
            public const int OverviewHeight = 150;
            public const int OverviewItemWidth = 240;
            public const int OverviewItemHeight = 240;
            public const int DetailsWidth = 440;
            public const int DetailsHeight = 240;
        }
    }
}