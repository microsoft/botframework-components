// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Prompts.Options;
using CalendarSkill.Responses.FindMeetingRoom;
using CalendarSkill.Responses.UpdateEvent;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;

namespace CalendarSkill.Dialogs
{
    public class UpdateMeetingRoomDialog : CalendarSkillDialogBase
    {
        public UpdateMeetingRoomDialog(
            IServiceProvider serviceProvider)
            : base(nameof(UpdateMeetingRoomDialog), serviceProvider)
        {
            var updateMeetingRoom = new WaterfallStep[]
            {
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                CheckFocusedEventAsync,
                FindMeetingRoomAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                UpdateMeetingRoomAsync
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

            // Define the conversation flow using a waterfall model.UpdateMeetingRoom
            AddDialog(new WaterfallDialog(Actions.UpdateMeetingRoom, updateMeetingRoom) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.FindEvent, findEvent) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.ChooseEvent, chooseEvent) { TelemetryClient = TelemetryClient });
            AddDialog(serviceProvider.GetService<FindMeetingRoomDialog>() ?? throw new ArgumentNullException(nameof(FindMeetingRoomDialog)));

            // Set starting dialog for component
            InitialDialogId = Actions.UpdateMeetingRoom;
        }

        private async Task<DialogTurnResult> FindMeetingRoomAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var options = (CalendarSkillDialogOptions)sc.Options;
                var origin = state.ShowMeetingInfo.FocusedEvents[0];
                var updateEvent = new EventModel(origin.Source);

                state.MeetingInfo.StartDateTime = origin.StartTime;
                state.MeetingInfo.EndDateTime = origin.EndTime;
                var ts = state.MeetingInfo.StartDateTime.Value.Subtract(state.MeetingInfo.EndDateTime.Value).Duration();
                state.MeetingInfo.Duration = (int)ts.TotalSeconds;

                if (state.InitialIntent == CalendarLuis.Intent.DeleteCalendarEntry)
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }

                return await sc.BeginDialogAsync(nameof(FindMeetingRoomDialog), sc.Options, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> UpdateMeetingRoomAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var origin = state.ShowMeetingInfo.FocusedEvents[0];
                var updateEvent = new EventModel(origin.Source);
                string meetingRoom = state.MeetingInfo.MeetingRoom?.DisplayName;
                var attendees = new List<EventModel.Attendee>();
                attendees.AddRange(origin.Attendees);

                if (state.InitialIntent == CalendarLuis.Intent.ChangeCalendarEntry)
                {
                    attendees.RemoveAll(x => x.AttendeeType == AttendeeType.Resource);
                }

                if (state.InitialIntent == CalendarLuis.Intent.DeleteCalendarEntry)
                {
                    meetingRoom = attendees.Find(x => x.AttendeeType == AttendeeType.Resource)?.DisplayName;
                    if (meetingRoom == null)
                    {
                        throw new Exception("No meeting room found.");
                    }

                    attendees.RemoveAll(x => x.AttendeeType == AttendeeType.Resource);
                }

                if (state.InitialIntent == CalendarLuis.Intent.ChangeCalendarEntry || state.InitialIntent == CalendarLuis.Intent.AddCalendarEntryAttribute)
                {
                    if (state.MeetingInfo.MeetingRoom == null)
                    {
                        throw new NullReferenceException("UpdateMeetingRoom received a null MeetingRoom.");
                    }

                    attendees.Add(new EventModel.Attendee
                    {
                        DisplayName = state.MeetingInfo.MeetingRoom.DisplayName,
                        Address = state.MeetingInfo.MeetingRoom.EmailAddress,
                        AttendeeType = AttendeeType.Resource
                    });
                }

                updateEvent.Id = origin.Id;
                updateEvent.Attendees = attendees;
                updateEvent.Location = null;
                if (!string.IsNullOrEmpty(state.UpdateMeetingInfo.RecurrencePattern) && !string.IsNullOrEmpty(origin.RecurringId))
                {
                    updateEvent.Id = origin.RecurringId;
                }

                sc.Context.TurnState.TryGetValue(StateProperties.APITokenKey, out var token);
                var calendarService = ServiceManager.InitCalendarService(token as string, state.EventSource);
                var newEvent = await calendarService.UpdateEventByIdAsync(updateEvent);

                var data = new
                {
                    MeetingRoom = meetingRoom,
                    DateTime = SpeakHelper.ToSpeechMeetingTime(TimeConverter.ConvertUtcToUserTime((DateTime)state.MeetingInfo.StartDateTime, state.GetUserTimeZone()), state.MeetingInfo.AllDay == true, DateTime.UtcNow > state.MeetingInfo.StartDateTime),
                    Subject = newEvent.Title,
                };
                if (state.InitialIntent == CalendarLuis.Intent.AddCalendarEntryAttribute)
                {
                    var replyMessage = await GetDetailMeetingResponseAsync(sc, newEvent, FindMeetingRoomResponses.MeetingRoomAdded, data, cancellationToken);
                    await sc.Context.SendActivityAsync(replyMessage, cancellationToken);
                }
                else if (state.InitialIntent == CalendarLuis.Intent.ChangeCalendarEntry)
                {
                    var replyMessage = await GetDetailMeetingResponseAsync(sc, newEvent, FindMeetingRoomResponses.MeetingRoomChanged, data, cancellationToken);
                    await sc.Context.SendActivityAsync(replyMessage, cancellationToken);
                }
                else
                {
                    var activity = TemplateManager.GenerateActivityForLocale(FindMeetingRoomResponses.MeetingRoomCanceled, data);
                    await sc.Context.SendActivityAsync(activity, cancellationToken);
                }

                state.Clear();
                return await sc.EndDialogAsync(true, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
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
                        Prompt = TemplateManager.GenerateActivityForLocale(UpdateEventResponses.NoUpdateStartTime),
                        RetryPrompt = TemplateManager.GenerateActivityForLocale(UpdateEventResponses.EventWithStartTimeNotFound),
                    }, cancellationToken);
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