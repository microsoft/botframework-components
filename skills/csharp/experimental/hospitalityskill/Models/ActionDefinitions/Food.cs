// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace HospitalitySkill.Models.ActionDefinitions
{
    public class Food
    {
        [JsonProperty("number")]
        public int Number { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("specialRequest")]
        public string SpecialRequest { get; set; }
    }
}
