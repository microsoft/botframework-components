// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ITSMSkill.Models;
using ITSMSkill.Models.Actions;
using ITSMSkill.Tests.API.Fakes;
using ITSMSkill.Tests.Flow.Strings;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using static Luis.ITSMLuis;

namespace ITSMSkill.Tests.Flow.Utterances
{
    public class TicketUpdateUtterances : ITSMTestUtterances
    {
        public static readonly string Update = "i would like to update a ticket";

        public static readonly string UpdateWithNumberUrgency = $"update ticket {MockData.CreateTicketNumber}'s urgency to {MockData.CreateTicketUrgency}";

        public static readonly Activity UpdateAction = new Activity(type: ActivityTypes.Event, name: ActionNames.UpdateTicket, value: JObject.FromObject(new
        {
        }));

        public static readonly Activity UpdateWithNumberUrgencyDescriptionAction = new Activity(type: ActivityTypes.Event, name: ActionNames.UpdateTicket, value: JObject.FromObject(new
        {
            number = MockData.CreateTicketNumber,
            urgency = NonLuisUtterances.CreateTicketUrgency,
            description = MockData.CreateTicketDescription,
        }));

        public TicketUpdateUtterances()
        {
            AddIntent(Update, Intent.TicketUpdate);
            AddIntent(UpdateWithNumberUrgency, Intent.TicketUpdate, attributeType: new string[][] { new string[] { AttributeType.Urgency.ToString() } }, urgencyLevel: new string[][] { new string[] { MockData.CreateTicketUrgencyLevel.ToString() } }, ticketNumber: new string[] { MockData.CreateTicketNumber });
        }
    }
}
