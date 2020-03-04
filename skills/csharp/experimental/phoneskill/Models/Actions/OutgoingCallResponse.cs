using Newtonsoft.Json;

namespace PhoneSkill.Models.Actions
{
    public class OutgoingCallResponse
    {
        [JsonProperty("actionSuccess")]
        public bool ActionSuccess { get; set; }
    }
}