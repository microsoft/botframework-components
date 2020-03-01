using Newtonsoft.Json;

namespace EmailSkill.Models.Action
{
    public class ActionResult
    {
        [JsonProperty("actionSuccess")]
        public bool ActionSuccess { get; set; }
    }
}
