// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Luis;

namespace MusicSkill.Models
{
    public class SkillState
    {
        public string Token { get; set; }

        public string Query { get; set; } = null;

        public bool IsAction { get; set; } = false;

        public void Clear()
        {
            Query = null;
            IsAction = false;
        }
    }
}
