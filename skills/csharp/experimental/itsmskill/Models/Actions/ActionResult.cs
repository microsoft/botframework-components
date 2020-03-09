// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace ITSMSkill.Models.Actions
{
    public class ActionResult
    {
        [JsonProperty("actionSuccess")]
        public bool ActionSuccess { get; set; }
    }
}
