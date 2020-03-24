using Newtonsoft.Json;

namespace BingSearchSkill.Models.Actions
{
    public class KeywordSearchInfo
    {
        [JsonProperty("keyword")]
        public string Keyword { get; set; }
    }
}
