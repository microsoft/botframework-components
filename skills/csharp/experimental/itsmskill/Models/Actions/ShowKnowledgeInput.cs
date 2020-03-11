// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;
using Newtonsoft.Json;

namespace ITSMSkill.Models.Actions
{
    public class ShowKnowledgeInput : IActionInput
    {
        [JsonProperty("search")]
        public string Search { get; set; }

        public override ITSMLuis CreateLuis()
        {
            var luis = new ITSMLuis
            {
                Entities = new ITSMLuis._Entities(),
            };

            if (!string.IsNullOrEmpty(Search))
            {
                luis.Entities.TicketTitle = new string[] { Search };
            }

            return luis;
        }
    }
}
