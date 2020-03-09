// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;

namespace ITSMSkill.Models.Actions
{
    public interface IActionInput
    {
        ITSMLuis Convert();
    }
}
