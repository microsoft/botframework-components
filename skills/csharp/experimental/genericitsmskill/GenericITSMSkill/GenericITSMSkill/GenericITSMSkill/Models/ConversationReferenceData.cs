using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace GenericITSMSkill.Models
{
    public class ConversationReferenceData : Document
    {
        [JsonProperty(PropertyName = "id")]
        public string ChannelID { get; set; }

        [JsonProperty(PropertyName = "ChannelConversationReferenceObject")]
        public ConversationReference ChannelConversationReferenceObject { get; set; }
    }
}
