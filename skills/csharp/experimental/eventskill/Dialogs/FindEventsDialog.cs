// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventSkill.Models;
using EventSkill.Models.Eventbrite;
using EventSkill.Responses.FindEvents;
using EventSkill.Services;
using EventSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Responses;

namespace EventSkill.Dialogs
{
    public class FindEventsDialog : EventDialogBase
    {
        private readonly EventbriteService _eventbriteService;

        public FindEventsDialog(
            IServiceProvider serviceProvider)
            : base(nameof(FindEventsDialog), serviceProvider)
        {
            var findEvents = new WaterfallStep[]
            {
                GetLocationAsync,
                FindEventsAsync
            };

            _eventbriteService = new EventbriteService(Settings);

            AddDialog(new WaterfallDialog(nameof(FindEventsDialog), findEvents));
            AddDialog(new TextPrompt(DialogIds.LocationPrompt, ValidateLocationPromptAsync));
        }

        private async Task<DialogTurnResult> GetLocationAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var convState = await StateAccessor.GetAsync(sc.Context, () => new EventSkillState(), cancellationToken: cancellationToken);
            var userState = await UserAccessor.GetAsync(sc.Context, () => new EventSkillUserState(), cancellationToken: cancellationToken);

            if (string.IsNullOrWhiteSpace(userState.Location))
            {
                if (!string.IsNullOrWhiteSpace(convState.CurrentCoordinates))
                {
                    userState.Location = convState.CurrentCoordinates;
                }
                else
                {
                    return await sc.PromptAsync(DialogIds.LocationPrompt, new PromptOptions()
                    {
                        Prompt = TemplateManager.GenerateActivity(FindEventsResponses.LocationPrompt),
                        RetryPrompt = TemplateManager.GenerateActivity(FindEventsResponses.RetryLocationPrompt)
                    }, cancellationToken);
                }
            }

            return await sc.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<bool> ValidateLocationPromptAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var userState = await UserAccessor.GetAsync(promptContext.Context, () => new EventSkillUserState(), cancellationToken: cancellationToken);
            if (promptContext.Recognized.Succeeded && !string.IsNullOrWhiteSpace(promptContext.Recognized.Value))
            {
                userState.Location = promptContext.Recognized.Value;
                return true;
            }

            return false;
        }

        private async Task<DialogTurnResult> FindEventsAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var userState = await UserAccessor.GetAsync(sc.Context, () => new EventSkillUserState(), cancellationToken: cancellationToken);

            var location = userState.Location;
            List<Event> events = await _eventbriteService.GetEventsAsync(location);
            List<Card> cards = new List<Card>();

            foreach (var item in events)
            {
                var eventCardData = new EventCardData()
                {
                    Title = item.Name.Text,
                    ImageUrl = item?.Logo?.Url ?? " ",
                    StartDate = item.Start.Local.ToString("dddd, MMMM dd, h:mm tt"),
                    Location = GetVenueLocation(item),
                    Price = item.IsFree ? "Free" : "Starts at " +
                        Convert.ToDouble(item.TicketAvailability.MinTicketPrice.MajorValue)
                        .ToString("C", System.Globalization.CultureInfo.GetCultureInfo(item.Locale.Replace("_", "-"))),
                    Url = item.Url
                };

                cards.Add(new Card(GetCardName(sc.Context, "EventCard"), eventCardData));
            }

            await sc.Context.SendActivityAsync(TemplateManager.GenerateActivity(FindEventsResponses.FoundEvents, cards, null), cancellationToken);

            return await sc.EndDialogAsync(cancellationToken: cancellationToken);
        }

        // Get formatted location string based on data event has
        private string GetVenueLocation(Event eventData)
        {
            string venueLocation = null;
            if (string.IsNullOrEmpty(eventData.Venue?.Address?.LocalizedAreaDisplay))
            {
                venueLocation = eventData.Venue.Name;
            }
            else if (string.IsNullOrEmpty(eventData.Venue?.Name))
            {
                venueLocation = eventData.Venue.Address.LocalizedAreaDisplay;
            }
            else
            {
                venueLocation = string.Format("{0}, {1}", eventData.Venue.Name, eventData.Venue.Address.LocalizedAreaDisplay);
            }

            return venueLocation;
        }

        private static class DialogIds
        {
            public const string LocationPrompt = "locationPrompt";
        }
    }
}
