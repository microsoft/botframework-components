using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewsSkill.Models.Action
{
    public class FavoriteTopicsInput
    {
        [JsonProperty("market")]
        public string Market { get; set; }

        [JsonProperty("category")]
        public string Category { get; set; }
    }
}
