﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace SkillServiceLibrary.Models.AzureMaps
{
    public class PoiInfo
    {
        [JsonProperty(PropertyName = "brands")]
        public PoiBrand[] Brands { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "classifications")]
        public Classification[] Classifications { get; set; }

        [JsonProperty(PropertyName = "phone")]
        public string Phone { get; set; }

        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }

        [JsonProperty(PropertyName = "openingHours")]
        public OpeningHours OpeningHours { get; set; }
    }
}
