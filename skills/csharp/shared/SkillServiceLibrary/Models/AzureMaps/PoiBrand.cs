// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace SkillServiceLibrary.Models.AzureMaps
{
    public class PoiBrand
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}
