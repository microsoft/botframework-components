// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace SkillServiceLibrary.Models.AzureMaps
{
    public class TimeRange
    {
        [JsonProperty(PropertyName = "startTime")]
        public Time StartTime { get; set; }

        [JsonProperty(PropertyName = "endTime")]
        public Time EndTime { get; set; }
    }
}
