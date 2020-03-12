// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using CalendarSkill.Responses.Shared;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Recognizers.Text.DateTime;
using static Microsoft.Recognizers.Text.Culture;

namespace CalendarSkill.Utilities
{
    public class DateTimeHelper
    {
        public static string ConvertNumberToDateTimeString(string numberString, bool convertToDate)
        {
            // convert exact number to date or time
            // if need convert to date, add "st", "rd", or "th" after the number
            // if need convert to time, add ":00" after the number
            if (int.TryParse(numberString, out var number))
            {
                if (convertToDate)
                {
                    if (number > 0 && number <= 31)
                    {
                        if (number % 10 == 1 && number != 11)
                        {
                            return string.Format(CalendarCommonStrings.OrdinalSuffixSt, numberString);
                        }
                        else if (number % 10 == 2 && number != 12)
                        {
                            return string.Format(CalendarCommonStrings.OrdinalSuffixNd, numberString);
                        }
                        else if (number % 10 == 3 && number != 13)
                        {
                            return string.Format(CalendarCommonStrings.OrdinalSuffixRd, numberString);
                        }
                        else
                        {
                            return string.Format(CalendarCommonStrings.OrdinalSuffixTh, numberString);
                        }
                    }
                }
                else
                {
                    if (number >= 0 && number <= 24)
                    {
                        return numberString + ":00";
                    }
                }
            }

            return numberString;
        }

        // StartTime.Count could be 1/2, while endTime.Count could be 0/1/2.
        public static DateTime ChooseStartTime(List<DateTime> startTimes, List<DateTime> endTimes, DateTime startTimeRestriction, DateTime endTimeRestriction, DateTime userNow)
        {
            // Only one startTime, return directly.
            if (startTimes.Count == 1)
            {
                return startTimes[0];
            }

            // Only one endTime, and startTimes[1] later than endTime. For example: start-11am/11pm, end-2pm, return the first one, while start-2am/2pm, end-3pm, return the second.
            if (endTimes.Count == 1)
            {
                return startTimes[1] > endTimes[0] ? startTimes[0] : startTimes[1];
            }

            // StartTimes[0] has passed. For example: "book a meeting from 6 to 7", and it's 10am now, we will use 6/7pm.
            if (startTimes[0] < userNow)
            {
                return startTimes[1];
            }

            // check which is valid by time restriction.
            if (IsInRange(startTimes[0], startTimeRestriction, endTimeRestriction))
            {
                return startTimes[0];
            }
            else if (IsInRange(startTimes[1], startTimeRestriction, endTimeRestriction))
            {
                return startTimes[1];
            }

            // default choose the first one.
            return startTimes[0];
        }

        public static bool IsInRange(DateTime time, DateTime startTime, DateTime endTime)
        {
            return startTime <= time && time <= endTime;
        }

        public static List<DateTime> GetDateFromDateTimeString(string date, string local, TimeZoneInfo userTimeZone, bool isStart, bool isTargetTimeRange)
        {
            // if isTargetTimeRange is true, will only parse the time range
            var culture = local ?? English;
            var results = RecognizeDateTime(date, culture, userTimeZone, true);
            var dateTimeResults = new List<DateTime>();
            if (results != null)
            {
                foreach (var result in results)
                {
                    if (result.Value != null)
                    {
                        if (isTargetTimeRange)
                        {
                            break;
                        }

                        var dateTime = DateTime.Parse(result.Value);
                        var dateTimeConvertType = result.Timex;

                        if (dateTime != null)
                        {
                            dateTimeResults.Add(dateTime);
                        }
                    }
                    else
                    {
                        var startDate = DateTime.Parse(result.Start);
                        var endDate = DateTime.Parse(result.End);
                        if (isStart)
                        {
                            dateTimeResults.Add(startDate);
                        }
                        else
                        {
                            dateTimeResults.Add(endDate);
                        }
                    }
                }
            }

            return dateTimeResults;
        }

        public static List<DateTime> GetTimeFromDateTimeString(string time, string local, TimeZoneInfo userTimeZone, bool isStart, bool isTargetTimeRange)
        {
            // if isTargetTimeRange is true, will only parse the time range
            var culture = local ?? English;
            var results = RecognizeDateTime(time, culture, userTimeZone, false);
            var dateTimeResults = new List<DateTime>();
            if (results != null)
            {
                foreach (var result in results)
                {
                    if (result.Value != null)
                    {
                        if (isTargetTimeRange)
                        {
                            break;
                        }

                        var dateTime = DateTime.Parse(result.Value);
                        var dateTimeConvertType = result.Timex;

                        if (dateTime != null)
                        {
                            dateTimeResults.Add(dateTime);
                        }
                    }
                    else
                    {
                        var startTime = DateTime.Parse(result.Start);
                        var endTime = DateTime.Parse(result.End);
                        if (isStart)
                        {
                            dateTimeResults.Add(startTime);
                        }
                        else
                        {
                            dateTimeResults.Add(endTime);
                        }
                    }
                }
            }

            return dateTimeResults;
        }

        public static List<DateTimeResolution> RecognizeDateTime(string dateTimeString, string culture, TimeZoneInfo userTimeZone, bool convertToDate = true)
        {
            var userNow = TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, userTimeZone);
            var results = DateTimeRecognizer.RecognizeDateTime(DateTimeHelper.ConvertNumberToDateTimeString(dateTimeString, convertToDate), culture, DateTimeOptions.CalendarMode, userNow);

            if (results.Count > 0)
            {
                // Return list of resolutions from first match
                var result = new List<DateTimeResolution>();
                var values = (List<Dictionary<string, string>>)results[0].Resolution["values"];
                foreach (var value in values)
                {
                    result.Add(ReadResolution(value));
                }

                return result;
            }

            return null;
        }

        public static TimeZoneInfo ConvertTimeZoneInfo(string timezone)
        {
            try
            {
                TimeZoneInfo result = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                return result;
            }
            catch
            {
                return null;
            }
        }

        private static DateTimeResolution ReadResolution(IDictionary<string, string> resolution)
        {
            var result = new DateTimeResolution();

            if (resolution.TryGetValue("timex", out var timex))
            {
                result.Timex = timex;
            }

            if (resolution.TryGetValue("value", out var value))
            {
                result.Value = value;
            }

            if (resolution.TryGetValue("start", out var start))
            {
                result.Start = start;
            }

            if (resolution.TryGetValue("end", out var end))
            {
                result.End = end;
            }

            return result;
        }
    }
}
