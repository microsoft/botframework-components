using Newtonsoft.Json;

namespace BingSearchSkill.Models.Actions
{
    public class ActionResult
    {
        public ActionResult(bool actionSuccess)
        {
            ActionSuccess = actionSuccess;
        }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("actionSuccess")]
        public bool ActionSuccess { get; set; }
    }
}
