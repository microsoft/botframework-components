using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BackToWorkSkill.Models
{
    public class ActionResult
    {
        public ActionResult(bool success)
        {
            ActionSuccess = success;
        }

        [JsonProperty("actionSuccess")]
        public bool ActionSuccess { get; set; }
    }
}
