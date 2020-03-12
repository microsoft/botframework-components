// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using HospitalitySkill.Models.ActionDefinitions;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using static Luis.HospitalityLuis;

namespace HospitalitySkill.Tests.Flow.Utterances
{
    public class RequestItemUtterances : HospitalityTestUtterances
    {
        public static readonly string Item = "Towel";

        public static readonly string InvalidItem = "TV";

        public static readonly string RequestItem = "i need something for my room";

        public static readonly string RequestWithItemAndInvalidItem = $"do you have {Item} and {InvalidItem}";

        public static readonly Activity RequestItemAction = new Activity(type: ActivityTypes.Event, name: ActionNames.RequestItem, value: JObject.FromObject(new
        {
        }));

        public static readonly Activity RequestWithItemAndInvalidItemAction = new Activity(type: ActivityTypes.Event, name: ActionNames.RequestItem, value: JObject.FromObject(new
        {
            items = new[]
            {
                new { number = 1, name = Item },
                new { number = 1, name = InvalidItem },
            }
        }));

        public RequestItemUtterances()
        {
            AddIntent(RequestItem, Intent.RequestItem);
            AddIntent(RequestWithItemAndInvalidItem, Intent.RequestItem, item: new string[] { Item, InvalidItem });
        }
    }
}
