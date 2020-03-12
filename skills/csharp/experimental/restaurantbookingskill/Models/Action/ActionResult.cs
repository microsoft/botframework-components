using Newtonsoft.Json;

namespace RestaurantBookingSkill.Models.Action
{
    public class ActionResult
    {
        [JsonProperty("actionSuccess")]
        public bool ActionSuccess { get; set; }
    }
}
