using Newtonsoft.Json;

namespace ToDoSkill.Models.Action
{
    public class ActionResult
    {
        [JsonProperty("actionSuccess")]
        public bool ActionSuccess { get; set; }
    }
}
