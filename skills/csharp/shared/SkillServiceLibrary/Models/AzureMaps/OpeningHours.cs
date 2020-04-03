// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace SkillServiceLibrary.Models.AzureMaps
{
    public class OpeningHours
    {
        [JsonProperty(PropertyName = "mode")]
        public OpeningHoursMode Mode { get; set; }

        [JsonProperty(PropertyName = "timeRanges")]
        public TimeRange[] TimeRanges { get; set; }
    }
}
