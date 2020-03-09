﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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

        public ITSMLuis Convert()
        {
            var luis = new ITSMLuis
            {
                Entities = new ITSMLuis._Entities(),
            };

            return luis;
        }
    }
}
