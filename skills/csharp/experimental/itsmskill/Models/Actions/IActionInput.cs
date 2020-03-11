// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;

namespace ITSMSkill.Models.Actions
{
    public class IActionInput
    {
        public virtual ITSMLuis CreateLuis()
        {
            return null;
        }

        public virtual void ProcessAfterDigest(SkillState state)
        {
        }
    }
}
