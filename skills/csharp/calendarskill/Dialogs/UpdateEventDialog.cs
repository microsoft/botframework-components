// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.ActionInfos;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Options;
using CalendarSkill.Prompts.Options;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Responses.UpdateEvent;
using CalendarSkill.Utilities;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;

namespace CalendarSkill.Dialogs
{
    public class UpdateEventDialog : CalendarSkillDialogBase
    {
        public UpdateEventDialog(
            IServiceProvider serviceProvider)
            : base(nameof(UpdateEventDialog), serviceProvider)
        {
            var updateEvent = new WaterfallStep[]
            {
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                CheckFocusedEventAsync,
                GetNewEventTimeAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                ConfirmBeforeUpdateAsync,
                AfterConfirmBeforeUpdateAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                UpdateEventTimeAsync
            };

            var findEvent = new WaterfallStep[]
            {
                SearchEventsWithEntitiesAsync,
                GetEventsPromptAsync,
                AfterGetEventsPromptAsync,
                AddConflictFlagAsync,
                ChooseEventAsync
            };

            var chooseEvent = new WaterfallStep[]
            {
                ChooseEventPromptAsync,
                AfterChooseEventAsync
            };

            var getNewStartTime = new WaterfallStep[]
            {
                GetNewEventTimePromptAsync,
                AfterGetNewEventTimePromptAsync,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.UpdateEventTime, updateEvent) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.FindEvent, findEvent) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.ChooseEvent, chooseEvent) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.GetNewStartTime, getNewStartTime) { TelemetryClient = TelemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.UpdateEventTime;
        }

        private async Task<DialogTurnResult> GetEventsPromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (state.ShowMeetingInfo.FocusedEvents.Any())
                {
                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }
                else if (state.ShowMeetingInfo.ShowingMeetings.Any())
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
                else
                {
                    sc.Context.TurnState.TryGetValue(StateProperties.APITokenKey, out var token);
                    var calendarService = ServiceManager.InitCalendarService(token as string, state.EventSource);
                    return await sc.PromptAsync(Actions.GetEventPrompt, new GetEventOptions(calendarService, state.GetUserTimeZone())
                    {
                        Prompt = TemplateManager.GenerateActivityForLocale(UpdateEventResponses.NoUpdateStartTime) as Activity,
                        RetryPrompt = TemplateManager.GenerateActivityForLocale(UpdateEventResponses.EventWithStartTimeNotFound) as Activity,
                        MaxReprompt = CalendarCommonUtil.MaxRepromptCount
                    }, cancellationToken);
                }
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> GetNewEventTimeAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.GetNewStartTime, sc.Options, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> ConfirmBeforeUpdateAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var newStartTime = (DateTime)state.UpdateMeetingInfo.NewStartDateTime;
                var origin = state.ShowMeetingInfo.FocusedEvents[0];
                var last = origin.EndTime - origin.StartTime;
                origin.StartTime = newStartTime;
                origin.EndTime = (newStartTime + last).AddSeconds(1);

                var replyMessage = await GetDetailMeetingResponseAsync(sc, origin, UpdateEventResponses.ConfirmUpdate, cancellationToken);

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = replyMessage,
                    RetryPrompt = TemplateManager.GenerateActivityForLocale(UpdateEventResponses.ConfirmUpdateFailed) as Activity,
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterConfirmBeforeUpdateAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var options = (CalendarSkillDialogOptions)sc.Options;
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
                else
                {
                    if (options.SubFlowMode)
                    {
                        state.UpdateMeetingInfo.Clear();
                    }
                    else
                    {
                        state.Clear();
                    }

                    return await sc.EndDialogAsync(true, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> UpdateEventTimeAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var options = (CalendarSkillDialogOptions)sc.Options;

                var newStartTime = TimeConverter.ConvertUtcToUserTime((DateTime)state.UpdateMeetingInfo.NewStartDateTime, state.GetUserTimeZone());
                var origin = state.ShowMeetingInfo.FocusedEvents[0];
                var updateEvent = new EventModel(origin.Source);
                var last = origin.EndTime - origin.StartTime;
                updateEvent.TimeZone = state.GetUserTimeZone();
                updateEvent.StartTime = newStartTime;
                updateEvent.EndTime = (newStartTime + last).AddSeconds(1);
                updateEvent.Id = origin.Id;

                if (!string.IsNullOrEmpty(state.UpdateMeetingInfo.RecurrencePattern) && !string.IsNullOrEmpty(origin.RecurringId))
                {
                    updateEvent.Id = origin.RecurringId;
                }

                sc.Context.TurnState.TryGetValue(StateProperties.APITokenKey, out var token);
                var calendarService = ServiceManager.InitCalendarService(token as string, state.EventSource);
                var newEvent = await calendarService.UpdateEventByIdAsync(updateEvent);

                var replyMessage = await GetDetailMeetingResponseAsync(sc, newEvent, UpdateEventResponses.EventUpdated, cancellationToken);

                await sc.Context.SendActivityAsync(replyMessage, cancellationToken);
                if (options != null && options.SubFlowMode)
                {
                    state.UpdateMeetingInfo.Clear();
                }

                if (state.IsAction)
                {
                    return await sc.EndDialogAsync(new ActionResult() { ActionSuccess = true }, cancellationToken);
                }

                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> GetNewEventTimePromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.UpdateMeetingInfo.NewStartDate.Any() || state.UpdateMeetingInfo.NewStartTime.Any() || state.UpdateMeetingInfo.MoveTimeSpan != 0)
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }

                return await sc.PromptAsync(Actions.TimePrompt, new TimePromptOptions
                {
                    Prompt = TemplateManager.GenerateActivityForLocale(UpdateEventResponses.NoNewTime) as Activity,
                    RetryPrompt = TemplateManager.GenerateActivityForLocale(UpdateEventResponses.NoNewTimeRetry) as Activity,
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

        private async Task<DialogTurnResult> AfterGetNewEventTimePromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.UpdateMeetingInfo.NewStartDate.Any() || state.UpdateMeetingInfo.NewStartTime.Any() || state.UpdateMeetingInfo.MoveTimeSpan != 0)
                {
                    var originalEvent = state.ShowMeetingInfo.FocusedEvents[0];
                    var originalStartDateTime = TimeConverter.ConvertUtcToUserTime(originalEvent.StartTime, state.GetUserTimeZone());
                    var userNow = TimeConverter.ConvertUtcToUserTime(DateTime.UtcNow, state.GetUserTimeZone());

                    if (state.UpdateMeetingInfo.NewStartDate.Any() || state.UpdateMeetingInfo.NewStartTime.Any())
                    {
                        var newStartDate = state.UpdateMeetingInfo.NewStartDate.Any() ?
                            state.UpdateMeetingInfo.NewStartDate.Last() :
                            originalStartDateTime;

                        var newStartTime = new List<DateTime>();
                        if (state.UpdateMeetingInfo.NewStartTime.Any())
                        {
                            newStartTime.AddRange(state.UpdateMeetingInfo.NewStartTime);
                        }
                        else
                        {
                            newStartTime.Add(originalStartDateTime);
                        }

                        foreach (var time in newStartTime)
                        {
                            var newStartDateTime = new DateTime(
                                newStartDate.Year,
                                newStartDate.Month,
                                newStartDate.Day,
                                time.Hour,
                                time.Minute,
                                time.Second);

                            if (state.UpdateMeetingInfo.NewStartDateTime == null)
                            {
                                state.UpdateMeetingInfo.NewStartDateTime = newStartDateTime;
                            }

                            if (newStartDateTime >= userNow)
                            {
                                state.UpdateMeetingInfo.NewStartDateTime = newStartDateTime;
                                break;
                            }
                        }
                    }
                    else if (state.UpdateMeetingInfo.MoveTimeSpan != 0)
                    {
                        state.UpdateMeetingInfo.NewStartDateTime = originalStartDateTime.AddSeconds(state.UpdateMeetingInfo.MoveTimeSpan);
                    }
                    else
                    {
                        return await sc.BeginDialogAsync(Actions.GetNewStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotFound), cancellationToken);
                    }

                    state.UpdateMeetingInfo.NewStartDateTime = TimeZoneInfo.ConvertTimeToUtc(state.UpdateMeetingInfo.NewStartDateTime.Value, state.GetUserTimeZone());

                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }
                else if (sc.Result != null)
                {
                    IList<DateTimeResolution> dateTimeResolutions = sc.Result as List<DateTimeResolution>;

                    DateTime? newStartTime = null;

                    foreach (var resolution in dateTimeResolutions)
                    {
                        var utcNow = DateTime.UtcNow;
                        var dateTimeConvertTypeString = resolution.Timex;
                        var dateTimeConvertType = new TimexProperty(dateTimeConvertTypeString);
                        var dateTimeValue = DateTime.Parse(resolution.Value);
                        if (dateTimeValue == null)
                        {
                            continue;
                        }

                        var originalStartDateTime = TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfo.FocusedEvents[0].StartTime, state.GetUserTimeZone());
                        if (dateTimeConvertType.Types.Contains(Constants.TimexTypes.Date) && !dateTimeConvertType.Types.Contains(Constants.TimexTypes.DateTime))
                        {
                            dateTimeValue = new DateTime(
                                dateTimeValue.Year,
                                dateTimeValue.Month,
                                dateTimeValue.Day,
                                originalStartDateTime.Hour,
                                originalStartDateTime.Minute,
                                originalStartDateTime.Second);
                        }
                        else if (dateTimeConvertType.Types.Contains(Constants.TimexTypes.Time) && !dateTimeConvertType.Types.Contains(Constants.TimexTypes.DateTime))
                        {
                            dateTimeValue = new DateTime(
                                originalStartDateTime.Year,
                                originalStartDateTime.Month,
                                originalStartDateTime.Day,
                                dateTimeValue.Hour,
                                dateTimeValue.Minute,
                                dateTimeValue.Second);
                        }

                        dateTimeValue = TimeZoneInfo.ConvertTimeToUtc(dateTimeValue, state.GetUserTimeZone());

                        if (newStartTime == null)
                        {
                            newStartTime = dateTimeValue;
                        }

                        if (dateTimeValue >= utcNow)
                        {
                            newStartTime = dateTimeValue;
                            break;
                        }
                    }

                    if (newStartTime != null)
                    {
                        state.UpdateMeetingInfo.NewStartDateTime = newStartTime;

                        return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                    }
                    else
                    {
                        return await sc.BeginDialogAsync(Actions.GetNewStartTime, new UpdateDateTimeDialogOptions(UpdateDateTimeDialogOptions.UpdateReason.NotADateTime), cancellationToken);
                    }
                }
                else
                {
                    // user has tried 5 times but can't get result
                    var activity = TemplateManager.GenerateActivityForLocale(CalendarSharedResponses.RetryTooManyResponse);
                    await sc.Context.SendActivityAsync(activity, cancellationToken);
                    return await sc.CancelAllDialogsAsync(cancellationToken);
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