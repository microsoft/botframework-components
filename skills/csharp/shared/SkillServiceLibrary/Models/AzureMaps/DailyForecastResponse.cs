// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace SkillServiceLibrary.Models.AzureMaps
{
    public class DailyForecastResponse
    {
        [JsonProperty(PropertyName = "forecasts")]
        public List<DailyForecast> Forecasts { get; set; }

        [JsonProperty(PropertyName = "summary")]
        public DailyForecastSummary Summary { get; set; }

    }
}
