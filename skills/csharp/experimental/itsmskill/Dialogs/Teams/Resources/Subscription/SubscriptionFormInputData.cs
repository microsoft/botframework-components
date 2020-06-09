using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ITSMSkill.Dialogs.Teams.Resources.Subscription
{
    public class SubscriptionFormInputData
    {
        public const string Action_Cancel = "Cancel";

        public string SeverityFilter { get; set; }

        public string Action { get; set; }
    }
}
