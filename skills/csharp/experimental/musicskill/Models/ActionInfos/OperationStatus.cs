using Newtonsoft.Json;

namespace MusicSkill.Models.ActionInfos
{
    public class OperationStatus
    {
        [JsonProperty("status")]
        public bool Status { get; set; }
    }
}
