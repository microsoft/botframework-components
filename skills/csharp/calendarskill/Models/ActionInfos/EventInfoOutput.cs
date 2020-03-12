using System;
using System.Collections.Generic;
using CalendarSkill.Utilities;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace CalendarSkill.Models.ActionInfos
{
    public class EventInfoOutput : ActionResult
    {
        public EventInfoOutput(EventModel eventModel, TimeZoneInfo userTimeZone, bool actionSuccess)
        {
            Title = eventModel.Title;
            Content = eventModel.Content;
            List<string> attendees = new List<string>();
            eventModel.Attendees.ForEach(a =>
            {
                if (a.AttendeeType != AttendeeType.Resource)
                {
                    attendees.Add(a.Address);
                }
            });
            Attendees = string.Join(",", attendees);
            StartDate = TimeConverter.ConvertUtcToUserTime(eventModel.StartTime, userTimeZone).ToString("yyyy-MM-dd");
            StartTime = TimeConverter.ConvertUtcToUserTime(eventModel.StartTime, userTimeZone).ToString("HH:mm");
            EndDate = TimeConverter.ConvertUtcToUserTime(eventModel.EndTime, userTimeZone).ToString("yyyy-MM-dd");
            EndTime = TimeConverter.ConvertUtcToUserTime(eventModel.EndTime, userTimeZone).ToString("HH:mm");
            Duration = (int)(eventModel.EndTime - eventModel.StartTime).TotalMinutes;
            Location = eventModel.Location;
            List<string> meetingRooms = new List<string>();
            eventModel.Attendees.ForEach(a =>
            {
                if (a.AttendeeType == AttendeeType.Resource)
                {
                    meetingRooms.Add(a.Address);
                }
            });
            MeetingRoom = string.Join(",", meetingRooms);
            Timezone = userTimeZone.Id;
            ActionSuccess = actionSuccess;
        }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("attendees")]
        public string Attendees { get; set; }

        [JsonProperty("startDate")]
        public string StartDate { get; set; }

        [JsonProperty("startTime")]
        public string StartTime { get; set; }

        [JsonProperty("endDate")]
        public string EndDate { get; set; }

        [JsonProperty("endTime")]
        public string EndTime { get; set; }

        [JsonProperty("duration")]
        public int Duration { get; set; }

        [JsonProperty("location")]
        public string Location { get; set; }

        [JsonProperty("meetingRoom")]
        public string MeetingRoom { get; set; }

        [JsonProperty("timezone")]
        public string Timezone { get; set; }
    }
}
