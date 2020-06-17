using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Solutions.Extensions.Model
{
    class MeetingModel
    {
        public bool IsConflict { get; set; } = false;

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public bool IsAccept { get; set; }

        public bool IsOrganizer { get; set; }

        public string Title { get; set; }

        public string Location { get; set; }

        public string Content { get; set; }

        public string OnlineMeetingUrl { get; set; }

        public string OnlineMeetingNumber { get; set; }

        public string OnlineMeetingCardInfo { get; set; }

        public string ID { get; set; }

        public List<Attendee> Attendees { get; set; }
    }
}
