using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GenericITSMSkill.Controllers.Helpers
{
    public class FlowHttpRequestData
    {
        public string StatusCode { get; set; }

        public string Title { get; set; }

        public string Id { get; set; }

        public string Description { get; set; }

        public string Status { get; set; }

        public string CreatedAt { get; set; }

        public string UpdatedAt { get; set; }

        public int NumOfComments { get; set; }

        [JsonProperty(PropertyName = "comments")]
        public string Comments { get; set; }

        [JsonProperty(PropertyName = "mentioned")]
        public List<string> Mentions { get; set; }
    }
}
