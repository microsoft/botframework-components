// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace SkillServiceLibrary.Models.AzureMaps
{
    public class HourlyForecastResponse
    {
        [JsonProperty(PropertyName = "forecasts")]
        public List<HourlyForecast> Forecasts { get; set; }
    }
}
