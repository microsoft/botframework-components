// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace SkillServiceLibrary.Models.AzureMaps
{
    public class WindDirection
    {
        [JsonProperty(PropertyName = "degrees")]
        public float Degrees { get; set; }

        [JsonProperty(PropertyName = "localizedDescription")]
        public string LocalizedDescription { get; set; }
    }
}
