// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace SkillServiceLibrary.Models.AzureMaps
{
    public class EntryPoint
    {
        [JsonProperty(PropertyName = "position")]
        public LatLng Position { get; set; }

        [JsonProperty(PropertyName = "type")]
        public EntryPointType Type { get; set; }
    }
}
