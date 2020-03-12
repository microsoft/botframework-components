// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CalendarSkill.Models.ActionInfos;
using Luis;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace CalendarSkill.Test.Flow.Utterances
{
    public class UpdateMeetingTestUtterances : BaseTestUtterances
    {
        public UpdateMeetingTestUtterances()
        {
            this.Add(BaseUpdateMeeting, GetBaseUpdateMeetingIntent(BaseUpdateMeeting));
            this.Add(UpdateMeetingWithStartTime, GetBaseUpdateMeetingIntent(
                UpdateMeetingWithStartTime,
                fromDate: new string[] { "tomorrow" },
                fromTime: new string[] { "6 pm" }));
            this.Add(UpdateMeetingWithTitle, GetBaseUpdateMeetingIntent(
                UpdateMeetingWithTitle,
                subject: new string[] { Strings.Strings.DefaultEventName }));
            this.Add(UpdateMeetingWithMoveEarlierTimeSpan, GetBaseUpdateMeetingIntent(
                UpdateMeetingWithMoveEarlierTimeSpan,
                subject: new string[] { Strings.Strings.DefaultEventName },
                moveEarlierTimeSpan: new string[] { "30 minutes" }));
            this.Add(UpdateMeetingWithMoveLaterTimeSpan, GetBaseUpdateMeetingIntent(
                UpdateMeetingWithMoveLaterTimeSpan,
                subject: new string[] { Strings.Strings.DefaultEventName },
                moveLaterTimeSpan: new string[] { "30 minutes" }));
            this.Add(UpdateMeetingWithNewStartDate, GetBaseUpdateMeetingIntent(
                UpdateMeetingWithNewStartDate,
                fromDate: new string[] { "tomorrow" },
                fromTime: new string[] { "6 pm" },
                toDate: new string[] { "today" }));
        }

        public static string BaseUpdateMeeting { get; } = "update meeting";

        public static string UpdateMeetingWithStartTime { get; } = "update meeting at tomorrow 6 pm";

        public static string UpdateMeetingWithNewStartDate { get; } = "update meeting at tomorrow 6 pm to today";

        public static string UpdateMeetingWithTitle { get; } = $"update {Strings.Strings.DefaultEventName} meeting";

        public static string UpdateMeetingWithMoveEarlierTimeSpan { get; } = $"update {Strings.Strings.DefaultEventName} meeting 30 minutes earlier";

        public static string UpdateMeetingWithMoveLaterTimeSpan { get; } = $"update {Strings.Strings.DefaultEventName} meeting 30 minutes later";

        public static string UpdateEventName { get; } = "UpdateEvent";

        public static Activity UpdateEventAction { get; } = new Activity()
        {
            Type = ActivityTypes.Event,
            Name = UpdateEventName,
            Value = JObject.FromObject(new UpdateEventInfo()
            {
                StartDate = "tomorrow",
                StartTime = "6 pm",
                NewStartDate = "today"
            })
        };

        public static CalendarLuis GetBaseUpdateMeetingIntent(
            string userInput,
            CalendarLuis.Intent intents = CalendarLuis.Intent.ChangeCalendarEntry,
            string[] subject = null,
            string[] fromDate = null,
            string[] toDate = null,
            string[] fromTime = null,
            string[] toTime = null,
            string[] moveEarlierTimeSpan = null,
            string[] moveLaterTimeSpan = null)
        {
            return GetCalendarIntent(
                userInput,
                intents,
                subject: subject,
                fromDate: fromDate,
                toDate: toDate,
                fromTime: fromTime,
                toTime: toTime,
                moveEarlierTimeSpan: moveEarlierTimeSpan,
                moveLaterTimeSpan: moveLaterTimeSpan);
        }
    }
}
