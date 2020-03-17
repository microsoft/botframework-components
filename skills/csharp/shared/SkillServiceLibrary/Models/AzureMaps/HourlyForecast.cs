// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace SkillServiceLibrary.Models.AzureMaps
{
    public class HourlyForecast
    {
        [JsonProperty(PropertyName = "date")]
        public string Date { get; set; }

        [JsonProperty(PropertyName = "iconCode")]
        public int IconCode { get; set; }

        [JsonProperty(PropertyName = "iconPhrase")]
        public string IconPhrase { get; set; }

        [JsonProperty(PropertyName = "isDaylight")]
        public bool IsDaylight { get; set; }

        [JsonProperty(PropertyName = "temperature")]
        public WeatherUnit Temperature { get; set; }

        [JsonProperty(PropertyName = "precipitationProbability")]
        public int PrecipitationProbability { get; set; }
    }
}
