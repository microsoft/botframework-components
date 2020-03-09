using Newtonsoft.Json;

namespace ToDoSkill.Models.Action
{
    public class ListInfo
    {
        [JsonProperty("listType")]
        public string ListType { get; set; }
    }
}
