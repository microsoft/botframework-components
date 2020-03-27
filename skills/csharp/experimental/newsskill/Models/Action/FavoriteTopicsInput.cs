// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace NewsSkill.Models.Action
{
    public class FavoriteTopicsInput
    {
        [JsonProperty("market")]
        public string Market { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }
    }
}
