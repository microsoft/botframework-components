// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace AutomotiveSkill.Models.Actions
{
    public class SettingInfo
    {
        [JsonProperty("setting")]
        public string SETTING { get; set; }

        [JsonProperty("value")]
        public string VALUE { get; set; }

        [JsonProperty("amount")]
        public string AMOUNT { get; set; }

        [JsonProperty("type")]
        public string TYPE { get; set; }

        [JsonProperty("unit")]
        public string UNIT { get; set; }

        public void DigestState(AutomotiveSkillState state)
        {
            if (AMOUNT != null)
            {
                state.AddEntities(nameof(AMOUNT), new List<string>() { AMOUNT });
            }

            if (SETTING != null)
            {
                state.AddEntities(nameof(SETTING), new List<string>() { SETTING });
            }

            if (TYPE != null)
            {
                state.AddEntities(nameof(TYPE), new List<string>() { TYPE });
            }

            if (UNIT != null)
            {
                state.AddEntities(nameof(UNIT), new List<string>() { UNIT });
            }

            if (VALUE != null)
            {
                state.AddEntities(nameof(VALUE), new List<string>() { VALUE });
            }
        }
    }
}
