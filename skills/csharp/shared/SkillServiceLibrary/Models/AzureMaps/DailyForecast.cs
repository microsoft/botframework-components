// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace SkillServiceLibrary.Models.AzureMaps
{
    public class DailyForecast
    {
        [JsonProperty(PropertyName = "date")]
        public string Date { get; set; }

        [JsonProperty(PropertyName = "temperature")]
        public WeatherUnitRange Temperature { get; set; }

        [JsonProperty(PropertyName = "day")]
        public DayOrNight Day { get; set; }

        [JsonProperty(PropertyName = "night")]
        public DayOrNight Night { get; set; }

        [JsonProperty(PropertyName = "sources")]
        public string[] Sources { get; set; }
    }
}
