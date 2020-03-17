// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace SkillServiceLibrary.Models.AzureMaps
{
    public class Wind
    {
        [JsonProperty(PropertyName = "direction")]
        public WindDirection Direction { get; set; }

        [JsonProperty(PropertyName = "speed")]
        public WindSpeed Speed { get; set; }
    }
}
