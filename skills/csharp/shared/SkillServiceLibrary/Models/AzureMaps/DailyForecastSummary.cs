// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace SkillServiceLibrary.Models.AzureMaps
{
    public class DailyForecastSummary
    {
        [JsonProperty(PropertyName = "phrase")]
        public string Phrase { get; set; }
    }
}
