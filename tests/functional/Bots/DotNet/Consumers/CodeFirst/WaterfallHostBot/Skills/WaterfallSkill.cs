// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Bot.Schema;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallHostBot.Skills
{
    public class WaterfallSkill : SkillDefinition
    {
        private enum SkillAction
        {
            Cards,
            Proactive,
            Auth,
            MessageWithAttachment,
            Sso,
            FileUpload,
            Echo,
            Delete,
            Update
        }

        public override IList<string> GetActions()
        {
            return Enum.GetNames(typeof(SkillAction));
        }

        public override Activity CreateBeginActivity(string actionId)
        {
            if (!Enum.TryParse<SkillAction>(actionId, true, out var skillAction))
            {
                throw new InvalidOperationException($"Unable to create begin activity for \"{actionId}\".");
            }

            // We don't support special parameters in these skills so a generic event with the right name
            // will do in this case.
            return new Activity(ActivityTypes.Event) { Name = skillAction.ToString() };
        }
    }
}
