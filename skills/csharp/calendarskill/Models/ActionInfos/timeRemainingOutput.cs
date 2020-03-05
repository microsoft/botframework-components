using Newtonsoft.Json;

namespace CalendarSkill.Models.ActionInfos
{
    public class TimeRemainingOutput
    {
        [JsonProperty("remainingTime")]
        public int RemainingTime { get; set; }
    }
}
