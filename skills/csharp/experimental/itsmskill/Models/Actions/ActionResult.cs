// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace ITSMSkill.Models.Actions
{
    public class ActionResult
    {
        public ActionResult(bool success)
        {
            ActionSuccess = success;
        }

        [JsonProperty("actionSuccess")]
        public bool ActionSuccess { get; set; }
    }
}
