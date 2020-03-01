using System.Collections.Generic;
using Newtonsoft.Json;

namespace WeatherSkill.Models.Action
{
    public class LocationInfo
    {
        [JsonProperty("location")]
        public string Location { get; set; }
    }
}
