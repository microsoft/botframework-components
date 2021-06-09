// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Connector;
using TranscriptTestRunner;

namespace SkillFunctionalTests.Common
{
    public class TestCase
    {
        public string Description { get; set; }

        public string ChannelId { get; set; }

        public string DeliveryMode { get; set; }

        public HostBot HostBot { get; set; }

        public string TargetSkill { get; set; }

        public string Script { get; set; }
    }
}
