﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using CalendarSkill.Models.ActionInfos;
using Luis;
using Microsoft.Bot.Schema;
using Newtonsoft.Json.Linq;

namespace CalendarSkill.Test.Flow.Utterances
{
    public class CreateMeetingTestUtterances : BaseTestUtterances
    {
        public CreateMeetingTestUtterances()
        {
            this.Add(BaseCreateMeeting, GetCreateMeetingIntent(BaseCreateMeeting));
            this.Add(CreateMeetingWithTitleEntity, GetCreateMeetingIntent(
                CreateMeetingWithTitleEntity,
                subject: new string[] { Strings.Strings.DefaultEventName }));
            this.Add(CreateMeetingWithOneContactEntity, GetCreateMeetingIntent(
                CreateMeetingWithOneContactEntity,
                contactName: new string[] { Strings.Strings.DefaultUserName }));
            this.Add(CreateMeetingWithDateTimeEntity, GetCreateMeetingIntent(
                CreateMeetingWithDateTimeEntity,
                fromDate: new string[] { Strings.Strings.DefaultStartDate },
                fromTime: new string[] { Strings.Strings.DefaultStartTime },
                toDate: new string[] { Strings.Strings.DefaultStartDate },
                toTime: new string[] { Strings.Strings.DefaultEndTime }));
            this.Add(CreateMeetingWithLocationEntity, GetCreateMeetingIntent(
                CreateMeetingWithLocationEntity,
                location: new string[] { Strings.Strings.DefaultLocation }));
            this.Add(CreateMeetingWithDurationEntity, GetCreateMeetingIntent(
                CreateMeetingWithDurationEntity,
                duration: new string[] { Strings.Strings.DefaultDuration }));
            this.Add(ChooseFirstUser, GetCreateMeetingIntent(
                ChooseFirstUser,
                intents: CalendarLuis.Intent.None,
                ordinal: new double[] { 1 }));
        }

        public static string BaseCreateMeeting { get; } = "Create a meeting";

        public static string CreateMeetingWithTitleEntity { get; } = $"Create a meeting about {Strings.Strings.DefaultEventName}";

        public static string CreateMeetingWithOneContactEntity { get; } = $"Create a meeting with {Strings.Strings.DefaultUserName}";

        public static string CreateMeetingWithDateTimeEntity { get; } = $"Create a meeting from {Strings.Strings.DefaultStartDate} {Strings.Strings.DefaultStartTime} to {Strings.Strings.DefaultStartDate} {Strings.Strings.DefaultEndTime}";

        public static string CreateMeetingWithLocationEntity { get; } = $"Create a meeting at {Strings.Strings.DefaultLocation}";

        public static string CreateMeetingWithDurationEntity { get; } = $"Create a meeting for {Strings.Strings.DefaultDuration}";

        public static string ChooseFirstUser { get; } = "the first";

        public static string CreateEventName { get; } = "CreateEvent";

        public static Activity CreateEventAction { get; } = new Activity()
        {
            Type = ActivityTypes.Event,
            Name = CreateEventName,
            Value = JObject.FromObject(new EventInfo()
            {
                Attendees = Strings.Strings.DefaultUserEmail,
                Content = Strings.Strings.DefaultContent,
                Duration = int.Parse(Strings.Strings.DefaultDurationMinutes),
                Location = Strings.Strings.DefaultLocation,
                StartDate = Strings.Strings.DefaultStartDate,
                StartTime = Strings.Strings.DefaultStartTime,
                Title = Strings.Strings.DefaultEventName
            })
        };

        private CalendarLuis GetCreateMeetingIntent(
            string userInput,
            CalendarLuis.Intent intents = CalendarLuis.Intent.CreateCalendarEntry,
            string[] subject = null,
            string[] contactName = null,
            string[] fromDate = null,
            string[] toDate = null,
            string[] fromTime = null,
            string[] toTime = null,
            string[] duration = null,
            string[] meetingRoom = null,
            string[] location = null,
            double[] ordinal = null,
            double[] number = null)
        {
            return GetCalendarIntent(
                userInput,
                intents,
                subject: subject,
                contactName: contactName,
                fromDate: fromDate,
                toDate: toDate,
                fromTime: fromTime,
                toTime: toTime,
                duration: duration,
                meetingRoom: meetingRoom,
                location: location,
                ordinal: ordinal,
                number: number);
        }
    }
}
