// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace MusicSkill.Models.ActionInfos
{
    public class SearchInfo
    {
        [JsonProperty("musicInfo")]
        public string MusicInfo { get; set; }

        public void DigestState(SkillState state)
        {
            state.Query = MusicInfo;
        }
    }
}
