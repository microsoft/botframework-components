// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace CalendarSkill.Models.ActionInfos
{
    public class TimeRemainingOutput : ActionResult
    {
        [JsonProperty("remainingTime")]
        public int RemainingTime { get; set; }
    }
}
