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
    public class TicketShowUtterances : ITSMTestUtterances
    {
        public static readonly string Show = "show my tickets";

        public static readonly string ShowWithTitle = $"show my tickets about {MockData.CreateTicketTitle}";

        public static readonly string ShowWithState = $"show my open tickets";

        public static readonly Activity ShowAction = new Activity(type: ActivityTypes.Event, name: ActionNames.ShowTicket, value: JObject.FromObject(new
        {
        }));

        public static readonly Activity ShowWithTitleAction = new Activity(type: ActivityTypes.Event, name: ActionNames.ShowTicket, value: JObject.FromObject(new
        {
            search = MockData.CreateTicketTitle,
        }));

        public TicketShowUtterances()
        {
            AddIntent(Show, Intent.TicketShow);
            AddIntent(ShowWithTitle, Intent.TicketShow, ticketTitle: new string[] { MockData.CreateTicketTitle });
            AddIntent(ShowWithState, Intent.TicketShow, ticketState: new string[][] { new string[] { TicketState.Active.ToString() } });
        }
    }
}
