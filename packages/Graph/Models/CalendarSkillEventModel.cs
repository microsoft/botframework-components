// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Components.Graph.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    public class CalendarSkillEventModel
    {
        public const string CalendarDescriptionString = "<br /><br /><span>📆 </span><span style =\"font-style: italic; font-family: 'Segoe UI', Calibri, sans-serif; font-size: 10px\">This event was created using the Calendar Skill.</span>";

        public CalendarSkillEventModel()
        {
        }

        public CalendarSkillEventModel(Event ev, DateTime currentDateTime, int index = 0, string userEmail = null)
        {
            var start = DateTime.Parse(ev.Start.DateTime);
            var end = DateTime.Parse(ev.End.DateTime);
            var duration = end.Subtract(start);
            var isCurrentEvent = false;

            if (start <= currentDateTime && (currentDateTime <= end
                || start.AddMinutes(-30) <= currentDateTime) && currentDateTime <= start)
            {
                // If event is currently ongoing, or will start in the next 30 minutes
                isCurrentEvent = true;
            }

            this.Index = index;
            this.Id = ev.Id;
            this.Subject = ev.Subject;
            this.Start = ev.Start;
            this.End = ev.End;
            this.Attendees = ev.Attendees.Where(a => a.EmailAddress.Address.ToLowerInvariant() != userEmail?.ToLowerInvariant());
            this.IsOnlineMeeting = ev.IsOnlineMeeting;
            this.OnlineMeeting = ev.OnlineMeeting;
            this.Description = ev.BodyPreview;
            this.Location = !string.IsNullOrEmpty(ev.Location.DisplayName) ? ev.Location.DisplayName : string.Empty;
            this.DurationDays = duration.Days;
            this.DurationHours = duration.Hours;
            this.DurationMinutes = duration.Minutes;
            this.IsRecurring = ev.Type == EventType.Occurrence || ev.Type == EventType.SeriesMaster ? true : false;
            this.IsCurrentEvent = isCurrentEvent;
            this.IsOrganizer = ev.IsOrganizer;
            this.IsAllDay = ev.IsAllDay;
            this.Response = ev.ResponseStatus.Response;
            this.Organizer = ev.Organizer;
            this.WebLink = ev.WebLink;
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

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "isAllDay", Required = Required.Default)]
        public bool? IsAllDay { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "response", Required = Required.Default)]
        public ResponseType? Response { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "organizer", Required = Required.Default)]
        public Recipient Organizer { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "webLink", Required = Required.Default)]
        public string WebLink { get; set; }
    }
}
