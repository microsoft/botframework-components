// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace EmailSkill.Models.Action
{
    public class SummaryResult
    {
        [JsonProperty("actionSuccess")]
        public bool ActionSuccess { get; set; }

        [JsonProperty("emailList")]
        public List<EmailInfo> EmailList { get; set; }
    }
}
