// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ITSMSkill.Models.ServiceNow
{
    using Newtonsoft.Json;

    /// <summary>
    /// ServiceNow Notification class.
    /// </summary>
    public class ServiceNowNotification
    {
        [JsonProperty]
        public string BusinessRuleName { get; set; }

        [JsonProperty]
        public string Id { get; set; }

        [JsonProperty]
        public string Title { get; set; }

        [JsonProperty]
        public string Description { get; set; }

        [JsonProperty]
        public string Category { get; set; }

        [JsonProperty]
        public string Impact { get; set; }

        [JsonProperty]
        public string Urgency { get; set; }

        [JsonProperty]
        public string UpdatedBy { get; set; }
    }
}
