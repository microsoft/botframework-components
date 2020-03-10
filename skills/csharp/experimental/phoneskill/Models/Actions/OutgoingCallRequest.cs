using Newtonsoft.Json;

namespace PhoneSkill.Models.Actions
{
    public class OutgoingCallRequest
    {
        [JsonProperty("contactPerson")]
        public string ContactPerson { get; set; }

        [JsonProperty("phoneNumber")]
        public string PhoneNumber { get; set; }
    }
}