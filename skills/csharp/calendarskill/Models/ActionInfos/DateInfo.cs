// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CalendarSkill.Utilities;
using Newtonsoft.Json;

namespace CalendarSkill.Models.ActionInfos
{
    public class DateInfo
    {
        [JsonProperty("date")]
        public string Date { get; set; }

        [JsonProperty("timezone")]
        public string Timezone { get; set; }

        public void DigestState(CalendarSkillState state)
        {
            if (!string.IsNullOrEmpty(Date))
            {
                state.MeetingInfo.StartDate = DateTimeHelper.GetDateFromDateTimeString(Date, null, state.GetUserTimeZone(), true, false);
            }

            if (!string.IsNullOrEmpty(Timezone))
            {
                state.UserInfo.Timezone = DateTimeHelper.ConvertTimeZoneInfo(Timezone);
            }
        }
    }
}
