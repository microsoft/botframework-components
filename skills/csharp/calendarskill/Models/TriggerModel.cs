using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalendarSkill.Models
{
    public class TriggerModel
    {
        public enum TriggerSource
        {
            /// <summary>
            /// Triggered by message
            /// </summary>
            Message = 1,

            /// <summary>
            /// Triggered by event
            /// </summary>
            Event = 2,
        }
    }
}
