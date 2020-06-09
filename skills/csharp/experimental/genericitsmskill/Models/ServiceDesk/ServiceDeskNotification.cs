using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GenericITSMSkill.Models.ServiceDesk
{
    public class ServiceDeskNotification
    {
        [JsonProperty]
        public string Id { get; set; }

        [JsonProperty]
        public string Title { get; set; }

        [JsonProperty]
        public string Description { get; set; }

        [JsonProperty]
        public string Category { get; set; }

        [JsonProperty]
        public string Impact { get; set; }

        [JsonProperty]
        public string Urgency { get; set; }

        public string ChannelId { get; set; }
    }
}
