﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace BingSearchSkill.Models.Actions
{
    public class KeywordSearchResponse
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("actionSuccess")]
        public bool ActionSuccess { get; set; }
    }
}
