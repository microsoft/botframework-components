using Newtonsoft.Json;

namespace CalendarSkill.Models.ActionInfos
{
    public class ActionResult
    {
        [JsonProperty("actionSuccess")]
        public bool ActionSuccess { get; set; }
    }
}
