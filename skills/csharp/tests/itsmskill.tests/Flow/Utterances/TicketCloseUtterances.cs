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
    public class TicketCloseUtterances : ITSMTestUtterances
    {
        public static readonly string Close = "close my ticket";

        public static readonly string CloseWithNumberReason = $"close {MockData.CloseTicketNumber} because {MockData.CloseTicketReason}";

        public static readonly Activity CloseAction = new Activity(type: ActivityTypes.Event, name: ActionNames.CloseTicket, value: JObject.FromObject(new
        {
        }));

        public static readonly Activity CloseWithNumberReasonAction = new Activity(type: ActivityTypes.Event, name: ActionNames.CloseTicket, value: JObject.FromObject(new
        {
            number = MockData.CloseTicketNumber,
            reason = MockData.CloseTicketReason,
        }));

        public TicketCloseUtterances()
        {
            AddIntent(Close, Intent.TicketClose);
            AddIntent(CloseWithNumberReason, Intent.TicketClose, closeReason: new string[] { MockData.CloseTicketReason }, ticketNumber: new string[] { MockData.CloseTicketNumber });
        }
    }
}
