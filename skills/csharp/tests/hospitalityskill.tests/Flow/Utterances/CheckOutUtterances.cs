// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using HospitalitySkill.Models.ActionDefinitions;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using static Luis.HospitalityLuis;

namespace HospitalitySkill.Tests.Flow.Utterances
{
    public class CheckOutUtterances : HospitalityTestUtterances
    {
        public static readonly string CheckOut = "can i check out";

        public static readonly Activity CheckOutAction = new Activity(type: ActivityTypes.Event, name: ActionNames.CheckOut, value: JObject.FromObject(new
        {
        }));

        public static readonly Activity CheckOutWithEmailAction = new Activity(type: ActivityTypes.Event, name: ActionNames.CheckOut, value: JObject.FromObject(new
        {
            Email = NonLuisUtterances.Email
        }));

        public CheckOutUtterances()
        {
            AddIntent(CheckOut, Intent.CheckOut);
        }
    }
}
