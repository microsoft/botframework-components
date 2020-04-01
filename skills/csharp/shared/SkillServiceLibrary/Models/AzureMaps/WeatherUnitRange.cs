// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace SkillServiceLibrary.Models.AzureMaps
{
    public class WeatherUnitRange
    {
        [JsonProperty(PropertyName = "minimum")]
        public WeatherUnit Minimum { get; set; }

        [JsonProperty(PropertyName = "maximum")]
        public WeatherUnit Maximum { get; set; }
    }
}
