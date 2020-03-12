using Newtonsoft.Json;

namespace PhoneSkill.Models.Actions
{
    public class ActionResult
    {
        [JsonProperty("actionSuccess")]
        public bool ActionSuccess { get; set; }
    }
}