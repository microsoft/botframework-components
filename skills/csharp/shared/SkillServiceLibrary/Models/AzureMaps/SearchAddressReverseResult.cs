using System.Collections.Generic;
using Newtonsoft.Json;

namespace SkillServiceLibrary.Models.AzureMaps
{
    public class SearchAddressReverseResult
    {
        [JsonProperty(PropertyName = "position")]
        public string Position { get; set; }

        [JsonProperty(PropertyName = "address")]
        public SearchAddress Address { get; set; }
    }
}
