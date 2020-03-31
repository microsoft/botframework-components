// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace SkillServiceLibrary.Models.AzureMaps
{
    public class WindSpeed
    {
        [JsonProperty(PropertyName = "value")]
        public float Value { get; set; }

        [JsonProperty(PropertyName = "unit")]
        public string Unit { get; set; }

        [JsonProperty(PropertyName = "unitType")]
        public int UnitType { get; set; }
    }
}
