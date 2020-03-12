using Newtonsoft.Json;

namespace CalendarSkill.Models.ActionInfos
{
    public class TimeRemainingOutput : ActionResult
    {
        [JsonProperty("remainingTime")]
        public int RemainingTime { get; set; }
    }
}
