// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using HospitalitySkill.Models.ActionDefinitions;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;
using static Luis.HospitalityLuis;

namespace HospitalitySkill.Tests.Flow.Utterances
{
    public class GetReservationUtterances : HospitalityTestUtterances
    {
        public static readonly string GetReservation = "what are my reservation details";

        public static readonly Activity GetReservationAction = new Activity(type: ActivityTypes.Event, name: ActionNames.GetReservationDetails, value: JObject.FromObject(new
        {
        }));

        public GetReservationUtterances()
        {
            AddIntent(GetReservation, Intent.GetReservationDetails);
        }
    }
}
