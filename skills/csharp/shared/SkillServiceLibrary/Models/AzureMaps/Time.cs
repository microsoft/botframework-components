// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Newtonsoft.Json;

namespace SkillServiceLibrary.Models.AzureMaps
{
    public class Time
    {
        [JsonProperty(PropertyName = "date")]
        public DateTime Date { get; set; }

        [JsonProperty(PropertyName = "hour")]
        public double Hour { get; set; }

        [JsonProperty(PropertyName = "minute")]
        public double Minute { get; set; }

        public DateTime ToDateTime()
        {
            return Date.AddHours(Hour).AddMinutes(Minute);
        }
    }
}
