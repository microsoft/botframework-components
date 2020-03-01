using Newtonsoft.Json;

namespace WeatherSkill.Models.Action
{
    public class ActionResult
    {
        [JsonProperty("actionSuccess")]
        public bool ActionSuccess { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }
    }
}
