using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.BotFramework.Composer.CustomAction.Models
{
    public class CalendarSkillEventModel
    {
        public CalendarSkillEventModel() { }

        public CalendarSkillEventModel(Event ev, DateTime currentDateTime, int index = 0)
        {
            var start = DateTime.Parse(ev.Start.DateTime);
            var end = DateTime.Parse(ev.End.DateTime);
            var duration = end.Subtract(start);
            var isCurrentEvent = false;

            if (start <= currentDateTime && currentDateTime <= end
                || start.AddMinutes(-30) <= currentDateTime && currentDateTime <= start)
            {
                // If event is currently ongoing, or will start in the next 30 minutes
                isCurrentEvent = true;
            }

            Index = index;
            Id = ev.Id;
            Subject = ev.Subject;
            Start = ev.Start;
            End = ev.End;
            Attendees = ev.Attendees;
            IsOnlineMeeting = ev.IsOnlineMeeting;
            OnlineMeeting = ev.OnlineMeeting;
            Description = ev.BodyPreview;
            Location = !string.IsNullOrEmpty(ev.Location.DisplayName) ? ev.Location.DisplayName : string.Empty;
            DurationDays = duration.Days;
            DurationHours = duration.Hours;
            DurationMinutes = duration.Minutes;
            IsRecurring = ev.Type == EventType.Occurrence || ev.Type == EventType.SeriesMaster ? true : false;
            IsCurrentEvent = isCurrentEvent;
            IsOrganizer = ev.IsOrganizer;
            Response = ev.ResponseStatus.Response;
            Organizer = ev.Organizer;
            WebLink = ev.WebLink;
        }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "index", Required = Required.Default)]
        public int Index { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "id", Required = Required.Default)]
        public string Id { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "subject", Required = Required.Default)]
        public string Subject { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "start", Required = Required.Default)]
        public DateTimeTimeZone Start { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "end", Required = Required.Default)]
        public DateTimeTimeZone End { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "attendees", Required = Required.Default)]
        public IEnumerable<Attendee> Attendees { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "isOnlineMeeting", Required = Required.Default)]
        public bool? IsOnlineMeeting { get; set; }
        
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "onlineMeeting", Required = Required.Default)]
        public OnlineMeetingInfo OnlineMeeting { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "description", Required = Required.Default)]
        public string Description { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "location", Required = Required.Default)]
        public string Location { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "durationDays", Required = Required.Default)]
        public int DurationDays { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "durationHours", Required = Required.Default)]
        public int DurationHours { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "durationMinutes", Required = Required.Default)]
        public int DurationMinutes { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "isRecurring", Required = Required.Default)]
        public bool IsRecurring { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "isCurrentEvent", Required = Required.Default)]
        public bool IsCurrentEvent { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "isOrganizer", Required = Required.Default)]
        public bool? IsOrganizer { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "response", Required = Required.Default)]
        public ResponseType? Response { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "organizer", Required = Required.Default)]
        public Recipient Organizer { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "webLink", Required = Required.Default)]
        public string WebLink { get; set; }
    }
}
