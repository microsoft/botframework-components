using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NewsSkill.Models.Action
{
    public class NewsResult
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("newsList")]
        public List<NewsInfo> NewsList { get; set; }

        [JsonProperty("actionSuccess")]
        public bool ActionSuccess { get; set; }
    }
}
