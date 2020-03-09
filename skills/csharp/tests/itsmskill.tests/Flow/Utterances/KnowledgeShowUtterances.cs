// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ITSMSkill.Models.Actions;
using ITSMSkill.Responses.Shared;
using ITSMSkill.Tests.API.Fakes;
using ITSMSkill.Tests.Flow.Strings;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using static Luis.ITSMLuis;

namespace ITSMSkill.Tests.Flow.Utterances
{
    public class KnowledgeShowUtterances : ITSMTestUtterances
    {
        public static readonly string Show = "search knowledgebase";

        public static readonly string ShowWithSearch = $"search knowledgebase about {MockData.CreateTicketTitle}";

        public static readonly Activity ShowAction = new Activity(type: ActivityTypes.Event, name: ActionNames.ShowKnowledge, value: JObject.FromObject(new
        {
        }));

        public static readonly Activity ShowWithSearchAction = new Activity(type: ActivityTypes.Event, name: ActionNames.ShowKnowledge, value: JObject.FromObject(new
        {
            search = MockData.CreateTicketTitle
        }));

        public KnowledgeShowUtterances()
        {
            AddIntent(Show, Intent.KnowledgeShow);
            AddIntent(ShowWithSearch, Intent.KnowledgeShow, ticketTitle: new string[] { MockData.CreateTicketTitle });
        }
    }
}
