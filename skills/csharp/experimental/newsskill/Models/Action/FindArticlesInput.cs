// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace NewsSkill.Models.Action
{
    public class FindArticlesInput
    {
        [JsonProperty("market")]
        public string Market { get; set; }

        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("site")]
        public string Site { get; set; }
    }
}
