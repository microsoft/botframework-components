// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using HospitalitySkill.Models.ActionDefinitions;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using static Luis.HospitalityLuis;
using static Luis.HospitalityLuis._Entities;

namespace HospitalitySkill.Tests.Flow.Utterances
{
    public class RoomServiceUtterances : HospitalityTestUtterances
    {
        public static readonly string Breakfast = "breakfast";

        public static readonly string Coffee = "coffee";

        public static readonly string RoomService = "i need some food";

        public static readonly string RoomServiceWithMenu = $"can i see a {Breakfast} menu";

        public static readonly string RoomServiceWithFood = $"i need a {Coffee}";

        public static readonly Activity RoomServiceAction = new Activity(type: ActivityTypes.Event, name: ActionNames.RoomService, value: JObject.FromObject(new
        {
        }));

        public static readonly Activity RoomServiceWithMenuAction = new Activity(type: ActivityTypes.Event, name: ActionNames.RoomService, value: JObject.FromObject(new
        {
            menu = Breakfast,
        }));

        public static readonly Activity RoomServiceWithFoodAction = new Activity(type: ActivityTypes.Event, name: ActionNames.RoomService, value: JObject.FromObject(new
        {
            food = new[] { new { number = 1, name = Coffee } }
        }));

        public RoomServiceUtterances()
        {
            AddIntent(Breakfast, Intent.None, menu: new string[][] { new string[] { Breakfast } });

            AddIntent(RoomService, Intent.RoomService);
            AddIntent(RoomServiceWithMenu, Intent.RoomService, menu: new string[][] { new string[] { Breakfast } });
            AddIntent(RoomServiceWithFood, Intent.RoomService, food: new string[] { Coffee });
        }
    }
}
