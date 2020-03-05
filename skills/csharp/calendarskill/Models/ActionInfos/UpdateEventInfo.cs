using CalendarSkill.Utilities;
using Newtonsoft.Json;

namespace CalendarSkill.Models.ActionInfos
{
    public class UpdateEventInfo
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("startDate")]
        public string StartDate { get; set; }

        [JsonProperty("startTime")]
        public string StartTime { get; set; }

        [JsonProperty("newStartDate")]
        public string NewStartDate { get; set; }

        [JsonProperty("newStartTime")]
        public string NewStartTime { get; set; }

        [JsonProperty("moveTimeSpan")]
        public int? MoveTimeSpan { get; set; }

        [JsonProperty("timezone")]
        public string Timezone { get; set; }

        public void DigestState(CalendarSkillState state)
        {
            state.MeetingInfo.Title = Title;
            if (!string.IsNullOrEmpty(Timezone))
            {
                state.UserInfo.Timezone = DateTimeHelper.ConvertTimeZoneInfo(Timezone);
            }

            if (!string.IsNullOrEmpty(StartDate))
            {
                state.MeetingInfo.StartDate = DateTimeHelper.GetDateFromDateTimeString(StartDate, null, state.GetUserTimeZone(), true, false);
            }

            if (!string.IsNullOrEmpty(StartTime))
            {
                state.MeetingInfo.StartTime = DateTimeHelper.GetDateFromDateTimeString(StartTime, null, state.GetUserTimeZone(), true, false);
            }

            if (!string.IsNullOrEmpty(NewStartDate))
            {
                state.UpdateMeetingInfo.NewStartDate = DateTimeHelper.GetDateFromDateTimeString(NewStartDate, null, state.GetUserTimeZone(), true, false);
            }

            if (!string.IsNullOrEmpty(NewStartTime))
            {
                state.UpdateMeetingInfo.NewStartTime = DateTimeHelper.GetDateFromDateTimeString(NewStartTime, null, state.GetUserTimeZone(), true, false);
            }

            if (MoveTimeSpan.GetValueOrDefault() != 0)
            {
                state.UpdateMeetingInfo.MoveTimeSpan = MoveTimeSpan.GetValueOrDefault() * 60;
            }
        }
    }
}
