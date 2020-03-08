using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewsSkill.Models.Action
{
    public class FindArticlesInput
    {
        [JsonProperty("market")]
        public string Market { get; set; }

        [JsonProperty("query")]
        public string Query { get; set; }

        [JsonProperty("site")]
        public string Site { get; set; }
    }
}
