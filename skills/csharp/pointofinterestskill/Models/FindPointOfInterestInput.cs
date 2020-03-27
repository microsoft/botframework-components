// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace PointOfInterestSkill.Models
{
    public class FindPointOfInterestInput : FindParkingInput
    {
        [JsonProperty("zipcode")]
        public string Zipcode { get; set; }

        [JsonProperty("countrySet")]
        public string CountrySet { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }

        public override void DigestActionInput(PointOfInterestSkillState state)
        {
            base.DigestActionInput(state);
            state.Category = Category;
        }
    }
}
