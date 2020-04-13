// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace MusicSkill.Models
{
    public class SkillState
    {
        public string Token { get; set; }

        public List<string> Genres { get; set; } = new List<string>();

        public string Query { get; set; } = null;

        public bool IsAction { get; set; } = false;

        public string ControlActionName { get; set; } = null;

        public string VolumeDirection { get; set; } = null;

        public void Clear()
        {
            Query = null;
            Genres.Clear();
            IsAction = false;
            VolumeDirection = null;
            ControlActionName = null;
        }
    }
}
