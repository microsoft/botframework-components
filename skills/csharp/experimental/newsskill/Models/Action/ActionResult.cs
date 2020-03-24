using System.Collections.Generic;
using Newtonsoft.Json;

namespace NewsSkill.Models.Action
{
    public class ActionResult
    {
        public ActionResult()
        {
        }

        public ActionResult(bool actionSuccess)
        {
            ActionSuccess = actionSuccess;
        }

        [JsonProperty("newsList")]
        public List<NewsInfo> NewsList { get; set; }

        [JsonProperty("actionSuccess")]
        public bool ActionSuccess { get; set; }
    }
}
