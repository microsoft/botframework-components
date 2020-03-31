// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Options;
using CalendarSkill.Responses.CheckPersonAvailable;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Utilities;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Extensions.DependencyInjection;

namespace CalendarSkill.Dialogs
{
    public class CheckPersonAvailableDialog : CalendarSkillDialogBase
    {
        public CheckPersonAvailableDialog(
            IServiceProvider serviceProvider)
            : base(nameof(CheckPersonAvailableDialog), serviceProvider)
        {
            var checkAvailable = new WaterfallStep[]
            {
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                CollectContactsAsync,
                CollectTimeAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                CheckAvailableAsync,
            };

            var collectTime = new WaterfallStep[]
            {
                AskForTimePromptAsync,
                AfterAskForTimePromptAsync
            };

            var findNextAvailableTime = new WaterfallStep[]
            {
                FindNextAvailableTimePromptAsync,
                AfterFindNextAvailableTimePromptAsync,
            };

            var createMeetingWithAvailableTime = new WaterfallStep[]
            {
                CreateMeetingPromptAsync,
                AfterCreateMeetingPromptAsync
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.CheckAvailable, checkAvailable) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.FindNextAvailableTime, findNextAvailableTime) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectTime, collectTime) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.CreateMeetingWithAvailableTime, createMeetingWithAvailableTime) { TelemetryClient = TelemetryClient });
            AddDialog(serviceProvider.GetService<FindContactDialog>() ?? throw new ArgumentNullException(nameof(FindContactDialog)));

            // Set starting dialog for component
            InitialDialogId = Actions.CheckAvailable;
        }

        private async Task<DialogTurnResult> CollectContactsAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(nameof(FindContactDialog), options: new FindContactDialogOptions(sc.Options), cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectTimeAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (!state.MeetingInfo.StartDate.Any() && !state.MeetingInfo.StartTime.Any())
                {
                    // when user say "Is Alex available", we think he mean right now.
                    // set time as the last five minutes time, for example now is 6:32, set start time as 6:35
                    var utcNow = DateTime.UtcNow;
                    var timeInterval = new TimeSpan(0, CalendarCommonUtil.AvailabilityViewInterval, 0);
                    var startTime = utcNow.AddTicks(timeInterval.Ticks - (utcNow.Ticks % timeInterval.Ticks));
                    state.MeetingInfo.StartTime.Add(TimeConverter.ConvertUtcToUserTime(startTime, state.GetUserTimeZone()));
                }

                if (state.MeetingInfo.StartTime.Any())
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
                else
                {
                    return await sc.BeginDialogAsync(Actions.CollectTime, options: sc.Options, cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AskForTimePromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                return await sc.PromptAsync(Actions.TimePrompt, new TimePromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivityForLocale(CheckPersonAvailableResponses.AskForCheckAvailableTime) as Activity,
                    RetryPrompt = TemplateManager.GenerateActivityForLocale(CheckPersonAvailableResponses.AskForCheckAvailableTime) as Activity,
                    TimeZone = state.GetUserTimeZone(),
                    MaxReprompt = CalendarCommonUtil.MaxRepromptCount
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterAskForTimePromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;
                foreach (var resolution in dateTimeResolutions)
                {
                    var dateTimeConvertType = resolution?.Timex;
                    var dateTimeValue = resolution?.Value;
                    if (dateTimeValue != null)
                    {
                        try
                        {
                            var dateTime = DateTime.Parse(dateTimeValue);

                            if (dateTime != null)
                            {
                                state.MeetingInfo.StartTime.Add(dateTime);
                            }
                        }
                        catch (FormatException ex)
                        {
                            await HandleExpectedDialogExceptionsAsync(sc, ex, cancellationToken);
                        }
                    }
                }

                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CheckAvailableAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var userNow = TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, state.GetUserTimeZone());
                var startDate = state.MeetingInfo.StartDate.Any() ? state.MeetingInfo.StartDate.Last() : userNow.Date;

                List<DateTime> startTimes = new List<DateTime>();
                List<DateTime> endTimes = new List<DateTime>();
                foreach (var time in state.MeetingInfo.StartTime)
                {
                    startTimes.Add(startDate.AddSeconds(time.TimeOfDay.TotalSeconds));
                }

                var isStartTimeRestricted = Settings.RestrictedValue?.MeetingTime?.First(item => item.Name == "WorkTimeStart")?.IsRestricted;
                var isEndTimeRestricted = Settings.RestrictedValue?.MeetingTime?.First(item => item.Name == "WorkTimeEnd")?.IsRestricted;
                DateTime baseTime = new DateTime(startDate.Year, startDate.Month, startDate.Day);
                DateTime startTimeRestricted = isStartTimeRestricted.GetValueOrDefault() ? baseTime.AddSeconds(DateTime.Parse(Settings.RestrictedValue?.MeetingTime?.First(item => item.Name == "WorkTimeStart")?.Value).TimeOfDay.TotalSeconds) : baseTime;
                DateTime endTimeRestricted = isEndTimeRestricted.GetValueOrDefault() ? baseTime.AddSeconds(DateTime.Parse(Settings.RestrictedValue?.MeetingTime?.First(item => item.Name == "WorkTimeEnd")?.Value).TimeOfDay.TotalSeconds) : baseTime.AddDays(1);

                state.MeetingInfo.StartDateTime = DateTimeHelper.ChooseStartTime(startTimes, endTimes, startTimeRestricted, endTimeRestricted, userNow);
                state.MeetingInfo.StartDateTime = TimeZoneInfo.ConvertTimeToUtc(state.MeetingInfo.StartDateTime.Value, state.GetUserTimeZone());

                sc.Context.TurnState.TryGetValue(StateProperties.APITokenKey, out var token);
                var calendarService = ServiceManager.InitCalendarService(token as string, state.EventSource);

                var dateTime = state.MeetingInfo.StartDateTime;

                var me = await GetMeAsync(sc.Context, cancellationToken);

                // the last one in result is the current user
                var availabilityResult = await calendarService.GetUserAvailabilityAsync(me.Emails[0], new List<string>() { state.MeetingInfo.ContactInfor.Contacts[0].Address }, dateTime.Value, CalendarCommonUtil.AvailabilityViewInterval);

                var timeString = TimeConverter.ConvertUtcToUserTime(state.MeetingInfo.StartDateTime.Value, state.GetUserTimeZone()).ToString(CommonStrings.DisplayTime);
                var startDateString = string.Empty;
                if (string.IsNullOrEmpty(state.MeetingInfo.StartDateString) ||
                    state.MeetingInfo.StartDateString.Equals(CalendarCommonStrings.TodayLower, StringComparison.InvariantCultureIgnoreCase) ||
                    state.MeetingInfo.StartDateString.Equals(CalendarCommonStrings.TomorrowLower, StringComparison.InvariantCultureIgnoreCase))
                {
                    startDateString = (state.MeetingInfo.StartDateString ?? CalendarCommonStrings.TodayLower).ToLower();
                }
                else
                {
                    startDateString = string.Format(CalendarCommonStrings.ShowEventDateCondition, state.MeetingInfo.StartDateString);
                }

                if (!availabilityResult.AvailabilityViewList.First().StartsWith("0"))
                {
                    // the attendee is not available
                    var activity = TemplateManager.GenerateActivityForLocale(CheckPersonAvailableResponses.NotAvailable, new
                    {
                        UserName = state.MeetingInfo.ContactInfor.Contacts[0].DisplayName ?? state.MeetingInfo.ContactInfor.Contacts[0].Address,
                        Time = timeString,
                        Date = startDateString
                    });
                    await sc.Context.SendActivityAsync(activity, cancellationToken);

                    state.MeetingInfo.AvailabilityResult = availabilityResult;

                    return await sc.BeginDialogAsync(Actions.FindNextAvailableTime, sc.Options, cancellationToken);
                }
                else
                {
                    // find the attendee's available time
                    var availableTime = 1;
                    var availabilityView = availabilityResult.AvailabilityViewList.First();
                    for (int i = 1; i < availabilityView.Length; i++)
                    {
                        if (availabilityView[i] == '0')
                        {
                            availableTime = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    availableTime *= CalendarCommonUtil.AvailabilityViewInterval;
                    var startAvailableTime = TimeConverter.ConvertUtcToUserTime(state.MeetingInfo.StartDateTime.Value, state.GetUserTimeZone());
                    var endAvailableTime = startAvailableTime.AddMinutes(availableTime);

                    // the current user may in non-working time
                    if (availabilityResult.AvailabilityViewList.Last().StartsWith("0") || availabilityResult.AvailabilityViewList.Last().StartsWith("3"))
                    {
                        // both attendee and current user is available
                        state.MeetingInfo.IsOrgnizerAvailable = true;

                        var activity = TemplateManager.GenerateActivityForLocale(CheckPersonAvailableResponses.AttendeeIsAvailable, new
                        {
                            StartTime = startAvailableTime.ToString(CommonStrings.DisplayTime),
                            EndTime = endAvailableTime.ToString(CommonStrings.DisplayTime),
                            Date = DisplayHelper.ToDisplayDate(endAvailableTime, state.GetUserTimeZone()),
                            UserName = state.MeetingInfo.ContactInfor.Contacts[0].DisplayName ?? state.MeetingInfo.ContactInfor.Contacts[0].Address
                        });
                        await sc.Context.SendActivityAsync(activity, cancellationToken);
                    }
                    else
                    {
                        // attendee is available but current user is not available
                        var conflictMeetingTitleList = new List<EventModel>();
                        foreach (var meeting in availabilityResult.MySchedule)
                        {
                            if (state.MeetingInfo.StartDateTime.Value >= meeting.StartTime && state.MeetingInfo.StartDateTime.Value < meeting.EndTime)
                            {
                                conflictMeetingTitleList.Add(meeting);
                            }
                        }

                        if (conflictMeetingTitleList.Count == 1)
                        {
                            var responseParams = new
                            {
                                StartTime = startAvailableTime.ToString(CommonStrings.DisplayTime),
                                EndTime = endAvailableTime.ToString(CommonStrings.DisplayTime),
                                Date = DisplayHelper.ToDisplayDate(endAvailableTime, state.GetUserTimeZone()),
                                UserName = state.MeetingInfo.ContactInfor.Contacts[0].DisplayName ?? state.MeetingInfo.ContactInfor.Contacts[0].Address,
                                Title = conflictMeetingTitleList.First().Title,
                                EventStartTime = TimeConverter.ConvertUtcToUserTime(conflictMeetingTitleList.First().StartTime, state.GetUserTimeZone()).ToString(CommonStrings.DisplayTime),
                                EventEndTime = TimeConverter.ConvertUtcToUserTime(conflictMeetingTitleList.First().EndTime, state.GetUserTimeZone()).ToString(CommonStrings.DisplayTime)
                            };
                            var activity = TemplateManager.GenerateActivityForLocale(CheckPersonAvailableResponses.AttendeeIsAvailableOrgnizerIsUnavailableWithOneConflict, responseParams);
                            await sc.Context.SendActivityAsync(activity, cancellationToken);
                        }
                        else
                        {
                            var responseParams = new
                            {
                                StartTime = startAvailableTime.ToString(CommonStrings.DisplayTime),
                                EndTime = endAvailableTime.ToString(CommonStrings.DisplayTime),
                                Date = DisplayHelper.ToDisplayDate(endAvailableTime, state.GetUserTimeZone()),
                                UserName = state.MeetingInfo.ContactInfor.Contacts[0].DisplayName ?? state.MeetingInfo.ContactInfor.Contacts[0].Address,
                                ConflictEventsCount = conflictMeetingTitleList.Count.ToString()
                            };
                            var activity = TemplateManager.GenerateActivityForLocale(CheckPersonAvailableResponses.AttendeeIsAvailableOrgnizerIsUnavailableWithMutipleConflicts, responseParams);
                            await sc.Context.SendActivityAsync(activity, cancellationToken);
                        }
                    }

                    return await sc.BeginDialogAsync(Actions.CreateMeetingWithAvailableTime, sc.Options, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> FindNextAvailableTimePromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                var data = new
                {
                    UserName = state.MeetingInfo.ContactInfor.Contacts[0].DisplayName ?? state.MeetingInfo.ContactInfor.Contacts[0].Address
                };

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = TemplateManager.GenerateActivityForLocale(CheckPersonAvailableResponses.AskForNextAvailableTime, data) as Activity,
                    RetryPrompt = TemplateManager.GenerateActivityForLocale(CheckPersonAvailableResponses.AskForNextAvailableTime, data) as Activity,
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterFindNextAvailableTimePromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    state.MeetingInfo.IsOrgnizerAvailable = true;
                    var availabilityResult = state.MeetingInfo.AvailabilityResult;

                    var startAvailableTimeIndex = -1;
                    var endAvailableTimeIndex = -1;

                    for (int i = 0; i < availabilityResult.AvailabilityViewList.First().Length; i++)
                    {
                        if (availabilityResult.AvailabilityViewList[0][i] == '0' && availabilityResult.AvailabilityViewList[1][i] == '0')
                        {
                            if (startAvailableTimeIndex < 0)
                            {
                                startAvailableTimeIndex = i;
                            }
                        }
                        else
                        {
                            if (startAvailableTimeIndex >= 0)
                            {
                                endAvailableTimeIndex = i - 1;
                                break;
                            }
                        }
                    }

                    if (startAvailableTimeIndex > 0)
                    {
                        endAvailableTimeIndex = endAvailableTimeIndex == -1 ? availabilityResult.AvailabilityViewList.First().Length - 1 : endAvailableTimeIndex;
                        var queryAvailableTime = state.MeetingInfo.StartDateTime.Value;

                        var startAvailableTime = queryAvailableTime.AddMinutes(startAvailableTimeIndex * CalendarCommonUtil.AvailabilityViewInterval);
                        var endAvailableTime = queryAvailableTime.AddMinutes((endAvailableTimeIndex + 1) * CalendarCommonUtil.AvailabilityViewInterval);

                        state.MeetingInfo.StartDateTime = startAvailableTime;

                        var activity = TemplateManager.GenerateActivityForLocale(CheckPersonAvailableResponses.NextBothAvailableTime, new
                        {
                            UserName = state.MeetingInfo.ContactInfor.Contacts[0].DisplayName ?? state.MeetingInfo.ContactInfor.Contacts[0].Address,
                            StartTime = TimeConverter.ConvertUtcToUserTime(state.MeetingInfo.StartDateTime.Value, state.GetUserTimeZone()).ToString(CommonStrings.DisplayTime),
                            EndTime = TimeConverter.ConvertUtcToUserTime(endAvailableTime, state.GetUserTimeZone()).ToString(CommonStrings.DisplayTime),
                            EndDate = DisplayHelper.ToDisplayDate(TimeConverter.ConvertUtcToUserTime(endAvailableTime, state.GetUserTimeZone()), state.GetUserTimeZone())
                        });
                        await sc.Context.SendActivityAsync(activity, cancellationToken);

                        return await sc.BeginDialogAsync(Actions.CreateMeetingWithAvailableTime, sc.Options, cancellationToken);
                    }

                    var activityNoNextBothAvailableTime = TemplateManager.GenerateActivityForLocale(CheckPersonAvailableResponses.NoNextBothAvailableTime, new
                    {
                        UserName = state.MeetingInfo.ContactInfor.Contacts[0].DisplayName ?? state.MeetingInfo.ContactInfor.Contacts[0].Address
                    });
                    await sc.Context.SendActivityAsync(activityNoNextBothAvailableTime, cancellationToken);

                    state.Clear();
                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }
                else
                {
                    state.Clear();
                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CreateMeetingPromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var responseId = state.MeetingInfo.IsOrgnizerAvailable ? CheckPersonAvailableResponses.AskForCreateNewMeeting : CheckPersonAvailableResponses.AskForCreateNewMeetingAnyway;
                var data = new
                {
                    UserName = state.MeetingInfo.ContactInfor.Contacts[0].DisplayName ?? state.MeetingInfo.ContactInfor.Contacts[0].Address,
                    StartTime = TimeConverter.ConvertUtcToUserTime(state.MeetingInfo.StartDateTime.Value, state.GetUserTimeZone()).ToString(CommonStrings.DisplayTime)
                };

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = TemplateManager.GenerateActivityForLocale(responseId, data) as Activity,
                    RetryPrompt = TemplateManager.GenerateActivityForLocale(responseId, data) as Activity,
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterCreateMeetingPromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    return await sc.BeginDialogAsync(nameof(CreateEventDialog), sc.Options, cancellationToken);
                }
                else
                {
                    state.Clear();
                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}
