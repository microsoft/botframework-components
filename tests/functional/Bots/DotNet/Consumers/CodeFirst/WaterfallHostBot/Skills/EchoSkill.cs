// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Bot.Schema;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallHostBot.Skills
{
    public class EchoSkill : SkillDefinition
    {
        private enum SkillAction
        {
            Message
        }

        public override IList<string> GetActions()
        {
            return new List<string> { SkillAction.Message.ToString() };
        }

        public override Activity CreateBeginActivity(string actionId)
        {
            if (!Enum.TryParse<SkillAction>(actionId, true, out _))
            {
                throw new InvalidOperationException($"Unable to create begin activity for \"{actionId}\".");
            }

            // We only support one activity for Echo so no further checks are needed
            return new Activity(ActivityTypes.Message)
            {
                Name = SkillAction.Message.ToString(),
                Text = "Begin the Echo Skill."
            };
        }
    }
}
