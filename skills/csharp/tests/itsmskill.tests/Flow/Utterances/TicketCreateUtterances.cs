// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ITSMSkill.Models.Actions;
using ITSMSkill.Tests.API.Fakes;
using ITSMSkill.Tests.Flow.Strings;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using static Luis.ITSMLuis;

namespace ITSMSkill.Tests.Flow.Utterances
{
    public class TicketCreateUtterances : ITSMTestUtterances
    {
        public static readonly string Create = "create a ticket";

        public static readonly string CreateWithTitleUrgency = $"create an urgency {NonLuisUtterances.CreateTicketUrgency} ticket about {MockData.CreateTicketTitle}";

        public static readonly Activity CreateAction = new Activity(type: ActivityTypes.Event, name: ActionNames.CreateTicket, value: JObject.FromObject(new
        {
        }));

        public static readonly Activity CreateWithTitleUrgencyDescriptionAction = new Activity(type: ActivityTypes.Event, name: ActionNames.CreateTicket, value: JObject.FromObject(new
        {
            urgency = NonLuisUtterances.CreateTicketUrgency,
            title = MockData.CreateTicketTitle,
            description = MockData.CreateTicketDescription,
        }));

        public TicketCreateUtterances()
        {
            AddIntent(Create, Intent.TicketCreate);
            AddIntent(CreateWithTitleUrgency, Intent.TicketCreate, urgencyLevel: new string[][] { new string[] { MockData.CreateTicketUrgencyLevel.ToString() } }, ticketTitle: new string[] { MockData.CreateTicketTitle });
        }
    }
}
