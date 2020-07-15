// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ITSMSkill.Models.ServiceNow
{
    /// <summary>
    /// ServiceNowBusinessRuleSubscription class to get user input.
    /// </summary>
    public class ServiceNowSubscription
    {
        public string FilterName { get; set; }

        public string NotificationNameSpace { get; set; }

        public string NotificationApiName { get; set; }

        public string FilterCondition { get; set; }
    }
}
