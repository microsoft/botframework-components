using Newtonsoft.Json;

namespace CalendarSkill.Models.ActionInfos
{
    public class OperationStatus
    {
        [JsonProperty("status")]
        public bool Status { get; set; }
    }
}
