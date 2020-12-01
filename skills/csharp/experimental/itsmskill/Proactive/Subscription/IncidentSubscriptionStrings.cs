using System.Collections.Generic;
using ITSMSkill.Models;

namespace ITSMSkill.Proactive.Subscription
{
    public class IncidentSubscriptionStrings
    {
        public static ICollection<string> FilterConditions { get; } = new[]
        {
            "Urgency",
            "Description",
            "Priority",
            "AssignedTo",
        };

        public static string FilterName = "FilterName";
        public static string NotificationNameSpace = "NotificationNameSpace";
        public static string PostNotificationAPIName = "PostNotificationAPIName";
        public static string SeverityTextBlock = "SeverityTextBlock";
    }
}
