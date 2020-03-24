using System;
using System.Collections.Generic;
using CalendarSkill.Utilities;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace CalendarSkill.Models.ActionInfos
{
    public class EventInfo
    {
        public EventInfo()
        {
        }

        public EventInfo(EventModel eventModel, TimeZoneInfo userTimeZone)
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

        [JsonProperty("building")]
        public string Building { get; set; }

        [JsonProperty("floorNumber")]
        public int? FloorNumber { get; set; }

        [JsonProperty("timezone")]
        public string Timezone { get; set; }

        public void DigestState(CalendarSkillState state)
        {
            state.MeetingInfo.Title = Title;
            state.MeetingInfo.Content = Content;
            if (!string.IsNullOrEmpty(Attendees))
            {
                state.MeetingInfo.ContactInfor.ContactsNameList = new List<string>(Attendees.Split(","));
            }

            if (!string.IsNullOrEmpty(Timezone))
            {
                state.UserInfo.Timezone = DateTimeHelper.ConvertTimeZoneInfo(Timezone);
            }

            if (!string.IsNullOrEmpty(StartDate))
            {
                state.MeetingInfo.StartDate = DateTimeHelper.GetDateFromDateTimeString(StartDate, null, state.GetUserTimeZone(), true, false);
            }

            if (!string.IsNullOrEmpty(EndDate))
            {
                state.MeetingInfo.EndDate = DateTimeHelper.GetDateFromDateTimeString(EndDate, null, state.GetUserTimeZone(), false, false);
            }

            if (!string.IsNullOrEmpty(StartTime))
            {
                state.MeetingInfo.StartTime = DateTimeHelper.GetDateFromDateTimeString(StartTime, null, state.GetUserTimeZone(), true, false);
            }

            if (!string.IsNullOrEmpty(EndTime))
            {
                state.MeetingInfo.EndTime = DateTimeHelper.GetDateFromDateTimeString(EndTime, null, state.GetUserTimeZone(), false, false);
            }

            state.MeetingInfo.Duration = Duration * 60;
            state.MeetingInfo.Location = Location;
            state.MeetingInfo.MeetingRoomName = MeetingRoom;
            state.MeetingInfo.Building = Building;
            state.MeetingInfo.FloorNumber = FloorNumber;
        }
    }
}
