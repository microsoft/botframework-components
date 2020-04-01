// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace SkillServiceLibrary.Models.AzureMaps
{
    public class DayOrNight
    {
        [JsonProperty(PropertyName = "iconCode")]
        public int IconCode { get; set; }

        [JsonProperty(PropertyName = "iconPhrase")]
        public string IconPhrase { get; set; }

        [JsonProperty(PropertyName = "shortPhrase")]
        public string ShortPhrase { get; set; }

        [JsonProperty(PropertyName = "longPhrase")]
        public string LongPhrase { get; set; }

        [JsonProperty(PropertyName = "wind")]
        public Wind Wind { get; set; }
    }
}
