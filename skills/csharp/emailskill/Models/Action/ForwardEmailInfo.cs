// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace EmailSkill.Models.Action
{
    public class ForwardEmailInfo
    {
        [JsonProperty("forwardReciever")]
        public List<string> ForwardReciever { get; set; }

        [JsonProperty("forwardMessage")]
        public string ForwardMessage { get; set; }
    }
}
