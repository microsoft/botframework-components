// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace EmailSkill.Models.Action
{
    public class ReplyEmailInfo
    {
        [JsonProperty("replyMessage")]
        public string ReplyMessage { get; set; }
    }
}
