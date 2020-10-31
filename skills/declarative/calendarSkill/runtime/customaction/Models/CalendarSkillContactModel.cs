using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.BotFramework.Composer.CustomAction.Models
{
    public class CalendarSkillContactModel
    {
        public string Name { get; set; }

        public List<string> EmailAddresses { get; set; }

        public string Id { get; set; }
    }
}
