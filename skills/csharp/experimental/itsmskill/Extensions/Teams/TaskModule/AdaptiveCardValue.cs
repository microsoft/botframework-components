// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ITSMSkill.Extensions.Teams.TaskModule
{
    using Newtonsoft.Json;

    /// <summary>
    /// DataContract on AdaptieCardValue
    /// </summary>
    public class AdaptiveCardValue<T>
    {
        [JsonProperty("data")]
        public T Data { get; set; }
    }
}
