// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace NewsSkill.Models
{
    public class NewsSkillState
    {
        public NewsSkillState()
        {
        }

        public Luis.NewsLuis LuisResult { get; set; }

        public string CurrentCoordinates { get; set; }

        public string Query { get; set; }

        public string Site { get; set; }

        public bool IsAction { get; set; } = false;

        public void Clear()
        {
            Query = null;
            Site = null;
            IsAction = false;
        }
    }
}
