using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.BotFramework.Composer.CustomAction.Models
{
    public class CalendarSkillEventModel
    {
        public string Id { get; set; }
        public string Subject { get; set; }
        public DateTimeTimeZone Start { get; set; }
        public DateTimeTimeZone End { get; set; }
        public IEnumerable<Attendee> Attendees { get; set; }
        public bool IsOnlineMeeting { get; set; }
        public OnlineMeetingInfo OnlineMeeting { get; set; }
        public string Description { get; set; }
        public string Location { get; set; }
        public int DurationDays { get; set; }
        public int DurationHours { get; set; }
        public int DurationMinutes { get; set; }
        public bool isRecurring { get; set; }
        public bool isCurrentEvent { get; set; }
        public bool IsOrganizer { get; set; }
        public string Response { get; set; }
        public Recipient Organizer { get; set; }
        public string WebLink { get; set; }
        public int index { get; set; }
    }
}
