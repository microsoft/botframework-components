// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;
using Newtonsoft.Json;

namespace ITSMSkill.Models.Actions
{
    public class ShowTicketInput : IActionInput
    {
        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("search")]
        public string Search { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("Urgency")]
        public string Urgency { get; set; }

        public override ITSMLuis CreateLuis()
        {
            var luis = new ITSMLuis
            {
                Entities = new ITSMLuis._Entities(),
            };

            if (!string.IsNullOrEmpty(Number))
            {
                luis.Entities.TicketNumber = new string[] { CovertNumber(Number) };
            }

            if (!string.IsNullOrEmpty(Search))
            {
                luis.Entities.TicketTitle = new string[] { Search };
            }

            if (!string.IsNullOrEmpty(State))
            {
                luis.Entities.TicketState = new string[][] { new string[] { State } };
            }

            if (!string.IsNullOrEmpty(Urgency))
            {
                luis.Entities.UrgencyLevel = new string[][] { new string[] { Urgency } };
            }

            return luis;
        }
    }
}
