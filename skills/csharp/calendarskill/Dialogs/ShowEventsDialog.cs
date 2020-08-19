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
using CalendarSkill.Responses.Shared;
using CalendarSkill.Responses.Summary;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Extensions.DependencyInjection;
using static CalendarSkill.Models.DialogOptions.ShowMeetingsDialogOptions;
using static Microsoft.Recognizers.Text.Culture;

namespace CalendarSkill.Dialogs
{
    public class ShowEventsDialog : CalendarSkillDialogBase
    {
        public ShowEventsDialog(
            IServiceProvider serviceProvider)
            : base(nameof(ShowEventsDialog), serviceProvider)
        {
            var showMeetings = new WaterfallStep[]
            {
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                SetSearchConditionAsync,
            };

            var searchEvents = new WaterfallStep[]
            {
                SearchEventsWithEntitiesAsync,
                FilterTodayEventAsync,
                AddConflictFlagAsync,
                ShowAskParameterDetailsAsync,
                ShowEventsListAsync
            };

            var showNextMeeting = new WaterfallStep[]
            {
                ShowNextMeetingAsync,
            };

            var showEventsOverview = new WaterfallStep[]
            {
                ShowEventsOverviewAsync,
                PromptForNextActionAsync,
                HandleNextActionAsync
            };

            var showEventsOverviewAgain = new WaterfallStep[]
            {
                ShowEventsOverviewAgainAsync,
                PromptForNextActionAsync,
                HandleNextActionAsync
            };

            var showFilteredEvents = new WaterfallStep[]
            {
                ShowFilteredEventsAsync,
                PromptForNextActionAsync,
                HandleNextActionAsync
            };

            var readEvent = new WaterfallStep[]
            {
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                ReadEventAsync,
                PromptForNextActionAfterReadAsync,
                HandleNextActionAfterReadAsync,
            };

            var updateEvent = new WaterfallStep[]
            {
                UpdateEventAsync,
                ReShowAsync
            };

            var changeEventStatus = new WaterfallStep[]
            {
                ChangeEventStatusAsync,
                ReShowAsync
            };

            var connectToMeeting = new WaterfallStep[]
            {
                ConnectToMeetingAsync,
                ReShowAsync
            };

            var reshow = new WaterfallStep[]
            {
                AskForShowOverviewAsync,
                AfterAskForShowOverviewAsync
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.ShowEvents, showMeetings) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.SearchEvents, searchEvents) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.ShowNextEvent, showNextMeeting) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.ShowEventsOverview, showEventsOverview) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.ShowEventsOverviewAgain, showEventsOverviewAgain) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.ShowFilteredEvents, showFilteredEvents) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.ChangeEventStatus, changeEventStatus) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateEvent, updateEvent) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.ConnectToMeeting, connectToMeeting) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.Read, readEvent) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.Reshow, reshow) { TelemetryClient = TelemetryClient });
            AddDialog(serviceProvider.GetService<UpdateEventDialog>() ?? throw new ArgumentNullException(nameof(UpdateEventDialog)));
            AddDialog(serviceProvider.GetService<ChangeEventStatusDialog>() ?? throw new ArgumentNullException(nameof(ChangeEventStatusDialog)));

            // Set starting dialog for component
            InitialDialogId = Actions.ShowEvents;
        }

        private async Task<DialogTurnResult> SetSearchConditionAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var options = sc.Options as ShowMeetingsDialogOptions;

                // if show next meeting
                if (state.MeetingInfo.OrderReference != null && state.MeetingInfo.OrderReference.ToLower().Contains(CalendarCommonStrings.Next))
                {
                    options.Reason = ShowMeetingReason.ShowNextMeeting;
                }
                else
                {
                    // set default search date
                    if (!state.MeetingInfo.StartDate.Any() && IsOnlySearchByTime(state))
                    {
                        state.MeetingInfo.StartDate.Add(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, state.GetUserTimeZone()));
                    }
                }

                return await sc.BeginDialogAsync(Actions.SearchEvents, options, cancellationToken);
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

        private bool IsOnlySearchByTime(CalendarSkillState state)
        {
            if (!string.IsNullOrEmpty(state.MeetingInfo.Title))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(state.MeetingInfo.Location))
            {
                return false;
            }

            if (state.MeetingInfo.ContactInfor.ContactsNameList.Any())
            {
                return false;
            }

            return true;
        }

        private async Task<DialogTurnResult> FilterTodayEventAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // if user query meetings in today, only show the meeting is upcoming in today.
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var isSearchedTodayMeeting = IsSearchedTodayMeeting(state);
                if (isSearchedTodayMeeting)
                {
                    var searchedEvents = new List<EventModel>();
                    foreach (var item in state.ShowMeetingInfo.ShowingMeetings)
                    {
                        if (item.StartTime >= DateTime.UtcNow)
                        {
                            searchedEvents.Add(item);
                        }
                    }

                    state.ShowMeetingInfo.ShowingMeetings = searchedEvents;
                }

                return await sc.NextAsync(cancellationToken: cancellationToken);
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

        private async Task<DialogTurnResult> ShowEventsListAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var options = sc.Options as ShowMeetingsDialogOptions;

                if (options != null && options.Reason == ShowMeetingReason.Summary && state.IsAction)
                {
                    List<EventInfo> eventInfos = new List<EventInfo>();
                    state.ShowMeetingInfo.ShowingMeetings.ForEach(e => eventInfos.Add(new EventInfo(e, state.GetUserTimeZone())));
                    return await sc.EndDialogAsync(new SummaryResult() { EventList = eventInfos, ActionSuccess = true }, cancellationToken);
                }

                // no meeting
                if (!state.ShowMeetingInfo.ShowingMeetings.Any())
                {
                    var activity = TemplateManager.GenerateActivityForLocale(SummaryResponses.ShowNoMeetingMessage);
                    await sc.Context.SendActivityAsync(activity, cancellationToken);
                    state.Clear();
                    return await sc.EndDialogAsync(true, cancellationToken);
                }

                if (options != null && options.Reason == ShowMeetingReason.ShowNextMeeting)
                {
                    return await sc.BeginDialogAsync(Actions.ShowNextEvent, options, cancellationToken);
                }
                else if (state.ShowMeetingInfo.ShowingMeetings.Count == 1)
                {
                    return await sc.BeginDialogAsync(Actions.Read, options, cancellationToken);
                }
                else if (options != null && (options.Reason == ShowMeetingReason.FirstShowOverview || options.Reason == ShowMeetingReason.ShowOverviewAfterPageTurning))
                {
                    return await sc.BeginDialogAsync(Actions.ShowEventsOverview, options, cancellationToken);
                }
                else if (options != null && options.Reason == ShowMeetingReason.ShowOverviewAgain)
                {
                    return await sc.BeginDialogAsync(Actions.ShowEventsOverviewAgain, options, cancellationToken);
                }

                return await sc.NextAsync(cancellationToken: cancellationToken);
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

        private async Task<DialogTurnResult> ShowAskParameterDetailsAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var options = sc.Options as ShowMeetingsDialogOptions;

                // this step will answer the question like: "when is my next meeting", next meeting is only one meeting will have answer
                if (state.ShowMeetingInfo.ShowingMeetings.Count == 1)
                {
                    var askParameter = new AskParameterModel(state.ShowMeetingInfo.AskParameterContent);
                    if (askParameter.NeedDetail)
                    {
                        var tokens = new
                        {
                            EventName = state.ShowMeetingInfo.ShowingMeetings[0].Title,
                            EventStartDate = TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfo.ShowingMeetings[0].StartTime, state.GetUserTimeZone()).ToString(CalendarCommonStrings.DisplayDateLong),
                            EventStartTime = TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfo.ShowingMeetings[0].StartTime, state.GetUserTimeZone()).ToString(CommonStrings.DisplayTime),
                            EventEndTime = TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfo.ShowingMeetings[0].EndTime, state.GetUserTimeZone()).ToString(CommonStrings.DisplayTime),
                            EventDuration = state.ShowMeetingInfo.ShowingMeetings[0].ToSpeechDurationString(),
                            EventLocation = state.ShowMeetingInfo.ShowingMeetings[0].Location
                        };

                        var activityBeforeShowEventDetails = TemplateManager.GenerateActivityForLocale(SummaryResponses.BeforeShowEventDetails, tokens);
                        await sc.Context.SendActivityAsync(activityBeforeShowEventDetails, cancellationToken);
                        if (askParameter.NeedTime)
                        {
                            var activity = TemplateManager.GenerateActivityForLocale(SummaryResponses.ReadTime, tokens);
                            await sc.Context.SendActivityAsync(activity, cancellationToken);
                        }

                        if (askParameter.NeedDuration)
                        {
                            var activity = TemplateManager.GenerateActivityForLocale(SummaryResponses.ReadDuration, tokens);
                            await sc.Context.SendActivityAsync(activity, cancellationToken);
                        }

                        if (askParameter.NeedLocation)
                        {
                            // for some event there might be no localtion.
                            if (string.IsNullOrEmpty(tokens.EventLocation))
                            {
                                var activity = TemplateManager.GenerateActivityForLocale(SummaryResponses.ReadNoLocation, tokens);
                                await sc.Context.SendActivityAsync(activity, cancellationToken);
                            }
                            else
                            {
                                var activity = TemplateManager.GenerateActivityForLocale(SummaryResponses.ReadLocation, tokens);
                                await sc.Context.SendActivityAsync(activity, cancellationToken);
                            }
                        }

                        if (askParameter.NeedDate)
                        {
                            var activity = TemplateManager.GenerateActivityForLocale(SummaryResponses.ReadStartDate, tokens);
                            await sc.Context.SendActivityAsync(activity, cancellationToken);
                        }
                    }
                }

                return await sc.NextAsync(cancellationToken: cancellationToken);
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

        private async Task<DialogTurnResult> ShowNextMeetingAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                // if only one next meeting, show the meeting detail card, otherwise show a meeting list card
                if (state.ShowMeetingInfo.ShowingMeetings.Count == 1)
                {
                    var speakParams = new
                    {
                        EventName = state.ShowMeetingInfo.ShowingMeetings[0].Title,
                        PeopleCount = state.ShowMeetingInfo.ShowingMeetings[0].Attendees.Count.ToString(),
                        EventTime = SpeakHelper.ToSpeechMeetingDateTime(
                            TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfo.ShowingMeetings[0].StartTime, state.GetUserTimeZone()),
                            state.ShowMeetingInfo.ShowingMeetings[0].IsAllDay == true),
                        Location = state.ShowMeetingInfo.ShowingMeetings[0].Location ?? string.Empty
                    };

                    string responseTemplateId = null;
                    if (string.IsNullOrEmpty(state.ShowMeetingInfo.ShowingMeetings[0].Location))
                    {
                        responseTemplateId = SummaryResponses.ShowNextMeetingNoLocationMessage;
                    }
                    else
                    {
                        responseTemplateId = SummaryResponses.ShowNextMeetingMessage;
                    }

                    var replyMessage = await GetDetailMeetingResponseAsync(sc, state.ShowMeetingInfo.ShowingMeetings.FirstOrDefault(), responseTemplateId, speakParams, cancellationToken);
                    await sc.Context.SendActivityAsync(replyMessage, cancellationToken);
                }
                else
                {
                    var activity = TemplateManager.GenerateActivityForLocale(SummaryResponses.ShowMultipleNextMeetingMessage);
                    await sc.Context.SendActivityAsync(activity, cancellationToken);
                    state.ShowMeetingInfo.ShowingCardTitle = CalendarCommonStrings.UpcommingMeeting;
                    var reply = await GetGeneralMeetingListResponseAsync(sc, state, true, cancellationToken: cancellationToken);
                    await sc.Context.SendActivityAsync(reply, cancellationToken);
                }

                var eventItem = state.ShowMeetingInfo.ShowingMeetings.FirstOrDefault();

                if (state.IsAction)
                {
                    EventInfoOutput eventInfoOutput = new EventInfoOutput(eventItem, state.GetUserTimeZone(), true);
                    return await sc.EndDialogAsync(eventInfoOutput, cancellationToken);
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

        private async Task<DialogTurnResult> ShowEventsOverviewAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var options = sc.Options as ShowMeetingsDialogOptions;
                string responseTemplateId = string.Empty;

                if (options.Reason == ShowMeetingReason.ShowOverviewAfterPageTurning)
                {
                    // show first meeting detail in response
                    var responseParams = new
                    {
                        Condition = GetSearchConditionString(state),
                        Count = state.ShowMeetingInfo.ShowingMeetings.Count.ToString(),
                        EventName1 = state.ShowMeetingInfo.ShowingMeetings[0].Title,
                        DateTime = state.MeetingInfo.StartDateString ?? CalendarCommonStrings.TodayLower,
                        EventTime1 = SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfo.ShowingMeetings[0].StartTime, state.GetUserTimeZone()), state.ShowMeetingInfo.ShowingMeetings[0].IsAllDay == true),
                        Participants1 = DisplayHelper.ToDisplayParticipantsStringSummary(state.ShowMeetingInfo.ShowingMeetings[0].Attendees, 1)
                    };
                    responseTemplateId = SummaryResponses.ShowMeetingSummaryNotFirstPageMessage;

                    await sc.Context.SendActivityAsync(await GetOverviewMeetingListResponseAsync(sc, responseTemplateId, responseParams, cancellationToken), cancellationToken);
                }
                else
                {
                    // if there are multiple meeting searched, show first and last meeting details in responses
                    var responseParams = new
                    {
                        Condition = GetSearchConditionString(state),
                        Count = state.ShowMeetingInfo.ShowingMeetings.Count.ToString(),
                        EventName1 = state.ShowMeetingInfo.ShowingMeetings[0].Title,
                        DateTime = state.MeetingInfo.StartDateString ?? CalendarCommonStrings.TodayLower,
                        EventTime1 = SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfo.ShowingMeetings[0].StartTime, state.GetUserTimeZone()), state.ShowMeetingInfo.ShowingMeetings[0].IsAllDay == true),
                        Participants1 = DisplayHelper.ToDisplayParticipantsStringSummary(state.ShowMeetingInfo.ShowingMeetings[0].Attendees, 1),
                        EventName2 = state.ShowMeetingInfo.ShowingMeetings[state.ShowMeetingInfo.ShowingMeetings.Count - 1].Title,
                        EventTime2 = SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfo.ShowingMeetings[state.ShowMeetingInfo.ShowingMeetings.Count - 1].StartTime, state.GetUserTimeZone()), state.ShowMeetingInfo.ShowingMeetings[state.ShowMeetingInfo.ShowingMeetings.Count - 1].IsAllDay == true),
                        Participants2 = DisplayHelper.ToDisplayParticipantsStringSummary(state.ShowMeetingInfo.ShowingMeetings[state.ShowMeetingInfo.ShowingMeetings.Count - 1].Attendees, 1)
                    };

                    if (state.ShowMeetingInfo.Condition == CalendarSkillState.ShowMeetingInfomation.SearchMeetingCondition.Time)
                    {
                        responseTemplateId = SummaryResponses.ShowMultipleMeetingSummaryMessage;
                    }
                    else
                    {
                        responseTemplateId = SummaryResponses.ShowMeetingSummaryShortMessage;
                    }

                    await sc.Context.SendActivityAsync(await GetOverviewMeetingListResponseAsync(sc, responseTemplateId, responseParams, cancellationToken), cancellationToken);
                }

                return await sc.NextAsync(cancellationToken: cancellationToken);
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

        private async Task<DialogTurnResult> ShowEventsOverviewAgainAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                // when show overview again, won't show meeting details in response
                var responseParams = new
                {
                    Count = state.ShowMeetingInfo.ShowingMeetings.Count.ToString(),
                    Condition = GetSearchConditionString(state)
                };
                var responseTemplateId = SummaryResponses.ShowMeetingSummaryShortMessage;

                await sc.Context.SendActivityAsync(await GetOverviewMeetingListResponseAsync(sc, responseTemplateId, responseParams, cancellationToken), cancellationToken);

                return await sc.NextAsync(cancellationToken: cancellationToken);
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

        private async Task<DialogTurnResult> ShowFilteredEventsAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                // show filtered meeting with general event list
                await sc.Context.SendActivityAsync(
                    await GetGeneralMeetingListResponseAsync(
                    sc, state, false,
                    SummaryResponses.ShowMultipleFilteredMeetings,
                    new { Count = state.ShowMeetingInfo.ShowingMeetings.Count.ToString() },cancellationToken),
                    cancellationToken);

                return await sc.NextAsync(cancellationToken: cancellationToken);
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

        private async Task<DialogTurnResult> PromptForNextActionAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.ShowMeetingInfo.ShowingMeetings.Count == 1)
                {
                    // if only one meeting is showing, the prompt text is already included in show events step, prompt an empty message here
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions(), cancellationToken);
                }

                var prompt = TemplateManager.GenerateActivityForLocale(SummaryResponses.ReadOutMorePrompt) as Activity;
                return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt }, cancellationToken);
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

        private async Task<DialogTurnResult> HandleNextActionAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;

                var luisResult = sc.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
                var generalLuisResult = sc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                var topIntent = luisResult?.TopIntent().intent;
                var generalTopIntent = generalLuisResult?.TopIntent().intent;
                generalTopIntent = MergeShowIntent(generalTopIntent, topIntent, luisResult);

                if (topIntent == null)
                {
                    return await sc.CancelAllDialogsAsync(cancellationToken);
                }

                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);
                if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true)
                {
                    // answer yes
                    state.ShowMeetingInfo.FocusedEvents.Add(state.ShowMeetingInfo.ShowingMeetings.First());
                    return await sc.BeginDialogAsync(Actions.Read, cancellationToken: cancellationToken);
                }
                else if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == false)
                {
                    // answer no
                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }

                if ((generalTopIntent == General.Intent.ShowNext || topIntent == CalendarLuis.Intent.ShowNextCalendar) && state.ShowMeetingInfo.ShowingMeetings != null)
                {
                    if ((state.ShowMeetingInfo.ShowEventIndex + 1) * state.PageSize < state.ShowMeetingInfo.ShowingMeetings.Count)
                    {
                        state.ShowMeetingInfo.ShowEventIndex++;
                    }
                    else
                    {
                        var activity = TemplateManager.GenerateActivityForLocale(SummaryResponses.CalendarNoMoreEvent);
                        await sc.Context.SendActivityAsync(activity, cancellationToken);
                    }

                    var options = sc.Options as ShowMeetingsDialogOptions;
                    options.Reason = ShowMeetingReason.ShowOverviewAfterPageTurning;
                    return await sc.ReplaceDialogAsync(Actions.ShowEvents, options, cancellationToken);
                }
                else if ((generalTopIntent == General.Intent.ShowPrevious || topIntent == CalendarLuis.Intent.ShowPreviousCalendar) && state.ShowMeetingInfo.ShowingMeetings != null)
                {
                    if (state.ShowMeetingInfo.ShowEventIndex > 0)
                    {
                        state.ShowMeetingInfo.ShowEventIndex--;
                    }
                    else
                    {
                        var activity = TemplateManager.GenerateActivityForLocale(SummaryResponses.CalendarNoPreviousEvent);
                        await sc.Context.SendActivityAsync(activity, cancellationToken);
                    }

                    var options = sc.Options as ShowMeetingsDialogOptions;
                    options.Reason = ShowMeetingReason.ShowOverviewAfterPageTurning;
                    return await sc.ReplaceDialogAsync(Actions.ShowEvents, options, cancellationToken);
                }
                else
                {
                    if (state.ShowMeetingInfo.ShowingMeetings.Count == 1)
                    {
                        state.ShowMeetingInfo.FocusedEvents.Add(state.ShowMeetingInfo.ShowingMeetings[0]);
                    }
                    else
                    {
                        var filteredMeetingList = GetFilteredEvents(state, luisResult, userInput, sc.Context.Activity.Locale ?? English, out var showingCardTitle);

                        if (filteredMeetingList.Count == 1)
                        {
                            state.ShowMeetingInfo.FocusedEvents = filteredMeetingList;
                        }
                        else if (filteredMeetingList.Count > 1)
                        {
                            state.ShowMeetingInfo.Clear();
                            state.ShowMeetingInfo.ShowingCardTitle = showingCardTitle;
                            state.ShowMeetingInfo.ShowingMeetings = filteredMeetingList;
                            return await sc.ReplaceDialogAsync(Actions.ShowFilteredEvents, sc.Options, cancellationToken);
                        }
                    }

                    var intentSwithingResult = await GetIntentSwitchingResultAsync(sc, topIntent.Value, state, cancellationToken);
                    if (intentSwithingResult != null)
                    {
                        return intentSwithingResult;
                    }
                    else
                    {
                        return await sc.BeginDialogAsync(Actions.Read, cancellationToken: cancellationToken);
                    }
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

        private async Task<DialogTurnResult> GetIntentSwitchingResultAsync(WaterfallStepContext sc, CalendarLuis.Intent topIntent, CalendarSkillState state, CancellationToken cancellationToken)
        {
            var newFlowOptions = new CalendarSkillDialogOptions() { SubFlowMode = false };
            if (topIntent == CalendarLuis.Intent.DeleteCalendarEntry || topIntent == CalendarLuis.Intent.AcceptEventEntry)
            {
                return await sc.BeginDialogAsync(Actions.ChangeEventStatus, cancellationToken: cancellationToken);
            }
            else if (topIntent == CalendarLuis.Intent.ChangeCalendarEntry)
            {
                return await sc.BeginDialogAsync(Actions.UpdateEvent, cancellationToken: cancellationToken);
            }
            else if (topIntent == CalendarLuis.Intent.CheckAvailability)
            {
                state.Clear();
                return await sc.ReplaceDialogAsync(nameof(CheckPersonAvailableDialog), newFlowOptions, cancellationToken);
            }
            else if (topIntent == CalendarLuis.Intent.ConnectToMeeting)
            {
                return await sc.BeginDialogAsync(Actions.ConnectToMeeting, cancellationToken: cancellationToken);
            }
            else if (topIntent == CalendarLuis.Intent.CreateCalendarEntry)
            {
                state.Clear();
                return await sc.ReplaceDialogAsync(nameof(CreateEventDialog), newFlowOptions, cancellationToken);
            }
            else if (topIntent == CalendarLuis.Intent.FindCalendarDetail
                || topIntent == CalendarLuis.Intent.FindCalendarEntry
                || topIntent == CalendarLuis.Intent.FindCalendarWhen
                || topIntent == CalendarLuis.Intent.FindCalendarWhere
                || topIntent == CalendarLuis.Intent.FindCalendarWho
                || topIntent == CalendarLuis.Intent.FindDuration
                || topIntent == CalendarLuis.Intent.FindMeetingRoom)
            {
                state.Clear();
                return await sc.ReplaceDialogAsync(nameof(ShowEventsDialog), new ShowMeetingsDialogOptions(ShowMeetingsDialogOptions.ShowMeetingReason.FirstShowOverview, newFlowOptions), cancellationToken);
            }
            else if (topIntent == CalendarLuis.Intent.TimeRemaining)
            {
                state.Clear();
                return await sc.ReplaceDialogAsync(nameof(TimeRemainingDialog), newFlowOptions, cancellationToken);
            }

            return null;
        }

        private async Task<DialogTurnResult> ReadEventAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // show a meeting detail card for the focused meeting
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var options = sc.Options as ShowMeetingsDialogOptions;

                // if isShowingMeetingDetail is true, will show the response of showing meeting detail. Otherwise will use show one summary meeting response.
                var isShowingMeetingDetail = true;

                if (!state.ShowMeetingInfo.FocusedEvents.Any())
                {
                    state.ShowMeetingInfo.FocusedEvents.Add(state.ShowMeetingInfo.ShowingMeetings.FirstOrDefault());
                    isShowingMeetingDetail = false;
                }

                var eventItem = state.ShowMeetingInfo.FocusedEvents.FirstOrDefault();

                if (isShowingMeetingDetail)
                {
                    var tokens = new
                    {
                        Date = eventItem.StartTime.ToString(CommonStrings.DisplayDateFormat_CurrentYear),
                        Time = SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(eventItem.StartTime, state.GetUserTimeZone()), eventItem.IsAllDay == true),
                        Participants = DisplayHelper.ToDisplayParticipantsStringSummary(eventItem.Attendees, 1),
                        Subject = eventItem.Title
                    };

                    var replyMessage = await GetDetailMeetingResponseAsync(sc, eventItem, SummaryResponses.ReadOutMessage, tokens, cancellationToken);
                    await sc.Context.SendActivityAsync(replyMessage, cancellationToken);
                }
                else
                {
                    var responseParams = new
                    {
                        Condition = GetSearchConditionString(state),
                        Count = state.ShowMeetingInfo.ShowingMeetings.Count.ToString(),
                        EventName1 = state.ShowMeetingInfo.ShowingMeetings[0].Title,
                        DateTime = state.MeetingInfo.StartDateString ?? CalendarCommonStrings.TodayLower,
                        EventTime1 = SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime(state.ShowMeetingInfo.ShowingMeetings[0].StartTime, state.GetUserTimeZone()), state.ShowMeetingInfo.ShowingMeetings[0].IsAllDay == true),
                        Participants1 = DisplayHelper.ToDisplayParticipantsStringSummary(state.ShowMeetingInfo.ShowingMeetings[0].Attendees, 1)
                    };
                    string responseTemplateId = null;

                    if (state.ShowMeetingInfo.ShowingMeetings.Count == 1)
                    {
                        if (state.ShowMeetingInfo.Condition == CalendarSkillState.ShowMeetingInfomation.SearchMeetingCondition.Time && !(options != null && options.Reason == ShowMeetingReason.ShowOverviewAgain))
                        {
                            responseTemplateId = SummaryResponses.ShowOneMeetingSummaryMessage;
                        }
                        else
                        {
                            responseTemplateId = SummaryResponses.ShowOneMeetingSummaryShortMessage;
                        }
                    }

                    var replyMessage = await GetDetailMeetingResponseAsync(sc, eventItem, responseTemplateId, responseParams, cancellationToken);
                    await sc.Context.SendActivityAsync(replyMessage, cancellationToken);
                }

                if (state.IsAction)
                {
                    EventInfoOutput eventInfoOutput = new EventInfoOutput(eventItem, state.GetUserTimeZone(), true);
                    return await sc.EndDialogAsync(eventInfoOutput, cancellationToken);
                }

                return await sc.NextAsync(cancellationToken: cancellationToken);
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

        private async Task<DialogTurnResult> PromptForNextActionAfterReadAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                var eventItem = state.ShowMeetingInfo.FocusedEvents.FirstOrDefault();

                if (eventItem.IsOrganizer)
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions
                    {
                        Prompt = TemplateManager.GenerateActivityForLocale(SummaryResponses.AskForOrgnizerAction, new { DateTime = state.MeetingInfo.StartDateString ?? CalendarCommonStrings.TodayLower }) as Activity
                    }, cancellationToken);
                }
                else if (eventItem.IsAccepted)
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions
                    {
                        Prompt = TemplateManager.GenerateActivityForLocale(SummaryResponses.AskForAction, new { DateTime = state.MeetingInfo.StartDateString ?? CalendarCommonStrings.TodayLower }) as Activity
                    }, cancellationToken);
                }
                else
                {
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions
                    {
                        Prompt = TemplateManager.GenerateActivityForLocale(SummaryResponses.AskForChangeStatus, new { DateTime = state.MeetingInfo.StartDateString ?? CalendarCommonStrings.TodayLower }) as Activity
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

        private async Task<DialogTurnResult> HandleNextActionAfterReadAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var luisResult = sc.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
                var topIntent = luisResult?.TopIntent().intent;

                sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);
                if (promptRecognizerResult.Succeeded && promptRecognizerResult.Value == true)
                {
                    state.ShowMeetingInfo.Clear();
                    return await sc.ReplaceDialogAsync(Actions.ShowEvents, new ShowMeetingsDialogOptions(ShowMeetingReason.ShowOverviewAgain, sc.Options), cancellationToken);
                }
                else
                {
                    var intentSwithingResult = await GetIntentSwitchingResultAsync(sc, topIntent.Value, state, cancellationToken);
                    if (intentSwithingResult != null)
                    {
                        return intentSwithingResult;
                    }
                }

                state.Clear();
                return await sc.CancelAllDialogsAsync(cancellationToken);
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

        private async Task<DialogTurnResult> ReShowAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.Reshow, cancellationToken: cancellationToken);
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

        private async Task<DialogTurnResult> UpdateEventAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var options = new CalendarSkillDialogOptions() { SubFlowMode = true };
                return await sc.BeginDialogAsync(nameof(UpdateEventDialog), options, cancellationToken);
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

        private async Task<DialogTurnResult> ChangeEventStatusAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var luisResult = sc.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
                var topIntent = luisResult?.TopIntent().intent;

                if (topIntent == CalendarLuis.Intent.AcceptEventEntry)
                {
                    var options = new ChangeEventStatusDialogOptions(new CalendarSkillDialogOptions() { SubFlowMode = true }, EventStatus.Accepted);
                    return await sc.BeginDialogAsync(nameof(ChangeEventStatusDialog), options, cancellationToken);
                }
                else
                {
                    var options = new ChangeEventStatusDialogOptions(new CalendarSkillDialogOptions() { SubFlowMode = true }, EventStatus.Cancelled);
                    return await sc.BeginDialogAsync(nameof(ChangeEventStatusDialog), options, cancellationToken);
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

        private async Task<DialogTurnResult> ConnectToMeetingAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var options = new CalendarSkillDialogOptions() { SubFlowMode = true };
                return await sc.BeginDialogAsync(nameof(JoinEventDialog), options, cancellationToken);
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

        private async Task<DialogTurnResult> AskForShowOverviewAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                state.ShowMeetingInfo.Clear();
                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = TemplateManager.GenerateActivityForLocale(SummaryResponses.AskForShowOverview, new { DateTime = state.MeetingInfo.StartDateString ?? CalendarCommonStrings.TodayLower }) as Activity,
                    RetryPrompt = TemplateManager.GenerateActivityForLocale(SummaryResponses.AskForShowOverview, new { DateTime = state.MeetingInfo.StartDateString ?? CalendarCommonStrings.TodayLower }) as Activity
                }, cancellationToken);
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

        private async Task<DialogTurnResult> AfterAskForShowOverviewAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var result = (bool)sc.Result;
                if (result)
                {
                    return await sc.ReplaceDialogAsync(Actions.ShowEvents, new ShowMeetingsDialogOptions(ShowMeetingReason.ShowOverviewAgain, sc.Options), cancellationToken);
                }
                else
                {
                    var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                    state.Clear();
                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
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

        private bool IsSearchedTodayMeeting(CalendarSkillState state)
        {
            var userNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, state.GetUserTimeZone());
            var searchDate = userNow;

            if (state.MeetingInfo.StartDate.Any())
            {
                searchDate = state.MeetingInfo.StartDate.Last();
            }

            return !state.MeetingInfo.StartTime.Any() &&
                !state.MeetingInfo.EndDate.Any() &&
                !state.MeetingInfo.EndTime.Any() &&
                EventModel.IsSameDate(searchDate, userNow);
        }
    }
}