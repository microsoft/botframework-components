// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Solutions;

namespace ITSMSkill.Services
{
    public class BotSettings : BotSettingsBase
    {
        public string ServiceNowUrl { get; set; }

        public string ServiceNowGetUserId { get; set; }

        public string ServiceNowNamespaceId { get; set; }

        public int LimitSize { get; set; }

        public bool FallbackToKnowledge { get; set; }
    }
}