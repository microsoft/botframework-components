// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;
using Newtonsoft.Json;

namespace ITSMSkill.Models.Actions
{
    public class CreateTicketInput : IActionInput
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("Urgency")]
        public string Urgency { get; set; }

        public override ITSMLuis CreateLuis()
        {
            var luis = new ITSMLuis
            {
                Entities = new ITSMLuis._Entities(),
            };

            if (!string.IsNullOrEmpty(Title))
            {
                luis.Entities.TicketTitle = new string[] { Title };
            }

            if (!string.IsNullOrEmpty(Urgency))
            {
                luis.Entities.UrgencyLevel = new string[][] { new string[] { Urgency } };
            }

            return luis;
        }

        public override void ProcessAfterDigest(SkillState state)
        {
            state.TicketDescription = Description;
        }
    }
}
