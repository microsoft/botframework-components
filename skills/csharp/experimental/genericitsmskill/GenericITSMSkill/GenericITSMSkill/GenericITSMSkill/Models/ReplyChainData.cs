using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Newtonsoft.Json;

namespace GenericITSMSkill.Models
{
    public class ReplyChainData : Document
    {
        [JsonProperty(PropertyName = "id")]
        public string ExternalTicketId { get; set; }

        [JsonProperty(PropertyName = "messageId")]
        public string MessageId { get; set; }
    }
}
