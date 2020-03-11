using Newtonsoft.Json;

namespace MusicSkill.Models.ActionInfos
{
    public class ActionResult
    {
        [JsonProperty("actionSuccess")]
        public bool ActionSuccess { get; set; }
    }
}
