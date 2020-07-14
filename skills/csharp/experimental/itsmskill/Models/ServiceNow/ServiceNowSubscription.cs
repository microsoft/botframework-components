using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Models.ServiceNow
{
    public class ServiceNowSubscription
    {
        public string FilterName { get; set; }

        public string NotificationNameSpace { get; set; }

        public string NotificationApiName { get; set; }

        public string FilterCondition { get; set; }
    }
}
