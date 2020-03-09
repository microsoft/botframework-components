using Luis;
using Newtonsoft.Json;

namespace MusicSkill.Models.ActionInfos
{
    public class SearchInfo
    {
        [JsonProperty("info")]
        public string Info { get; set; }

        public void DigestState(SkillState state)
        {
            state.Query = Info;
        }
    }
}
