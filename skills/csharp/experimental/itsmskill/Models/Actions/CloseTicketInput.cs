// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;
using Newtonsoft.Json;

namespace ITSMSkill.Models.Actions
{
    public class CloseTicketInput : IActionInput
    {
        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }

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

            if (!string.IsNullOrEmpty(Reason))
            {
                luis.Entities.CloseReason = new string[] { Reason };
            }

            return luis;
        }
    }
}
