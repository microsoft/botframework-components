// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace EmailSkill.Models.Action
{
    public class EmailInfo
    {
        [JsonProperty("subject")]
        public string Subject { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("sender")]
        public string Sender { get; set; }

        [JsonProperty("reciever")]
        public List<string> Reciever { get; set; }
    }
}