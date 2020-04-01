// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace SkillServiceLibrary.Models.AzureMaps
{
    public class SearchAddressReverseResponse
    {
        [JsonProperty(PropertyName = "addresses")]
        public List<SearchAddressReverseResult> Addresses { get; set; }
    }
}