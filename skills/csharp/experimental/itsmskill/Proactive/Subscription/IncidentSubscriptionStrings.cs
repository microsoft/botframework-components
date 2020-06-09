using System.Collections.Generic;
using ITSMSkill.Models;

namespace ITSMSkill.Proactive.Subscription
{
    public class IncidentSubscriptionStrings
    {
        public static ICollection<string> Severities { get; } = new[]
        {
            UrgencyLevel.High.ToString(),
            UrgencyLevel.Medium.ToString(),
            UrgencyLevel.Low.ToString(),
            UrgencyLevel.None.ToString()
        };

        public static string SeverityTextBlock = "SeverityTextBlock";
    }
}
