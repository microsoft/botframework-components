// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CalendarSkill.Responses.Shared;
using CalendarSkill.Utilities;
using Newtonsoft.Json;

namespace CalendarSkill.Models.ActionInfos
{
    public class ChooseEventInfo
    {
        [JsonProperty("nextEvent")]
        public bool NextEvent { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("startDate")]
        public string StartDate { get; set; }

        [JsonProperty("startTime")]
        public string StartTime { get; set; }

        [JsonProperty("timezone")]
        public string Timezone { get; set; }

        public void DigestState(CalendarSkillState state)
        {
            state.MeetingInfo.Title = Title;
            if (!string.IsNullOrEmpty(Timezone))
            {
                state.UserInfo.Timezone = DateTimeHelper.ConvertTimeZoneInfo(Timezone);
            }

            if (NextEvent == true)
            {
                state.MeetingInfo.OrderReference = CalendarCommonStrings.Next;
            }

            if (!string.IsNullOrEmpty(StartDate))
            {
                state.MeetingInfo.StartDate = DateTimeHelper.GetDateFromDateTimeString(StartDate, null, state.GetUserTimeZone(), true, false);
                state.MeetingInfo.StartDateString = StartDate;
            }

            if (!string.IsNullOrEmpty(StartTime))
            {
                state.MeetingInfo.StartTime = DateTimeHelper.GetDateFromDateTimeString(StartTime, null, state.GetUserTimeZone(), true, false);
            }
        }
    }
}
