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
using CalendarSkill.Prompts.Options;
using CalendarSkill.Responses.JoinEvent;
using CalendarSkill.Utilities;
using HtmlAgilityPack;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Models;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Newtonsoft.Json;

namespace CalendarSkill.Dialogs
{
    public class JoinEventDialog : CalendarSkillDialogBase
    {
        public JoinEventDialog(
            IServiceProvider serviceProvider)
            : base(nameof(JoinEventDialog), serviceProvider)
        {
            var joinMeeting = new WaterfallStep[]
            {
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                CheckFocusedEventAsync,
                ConfirmNumberAsync,
                AfterConfirmNumberAsync
            };

            var findEvent = new WaterfallStep[]
            {
                SearchEventsWithEntitiesAsync,
                GetEventsAsync,
                AddConflictFlagAsync,
                ChooseEventAsync
            };

            var getEvents = new WaterfallStep[]
            {
                GetEventsPromptAsync,
                AfterGetEventsPromptAsync,
                CheckValidAsync,
            };

            var chooseEvent = new WaterfallStep[]
            {
                ChooseEventPromptAsync,
                AfterChooseEventAsync
            };

            AddDialog(new WaterfallDialog(Actions.ConnectToMeeting, joinMeeting) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.GetEvents, getEvents) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.FindEvent, findEvent) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.ChooseEvent, chooseEvent) { TelemetryClient = TelemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.ConnectToMeeting;
        }

        private string GetDialInNumberFromMeeting(EventModel eventModel)
        {
            // Support teams and skype meeting.
            if (string.IsNullOrEmpty(eventModel.Content))
            {
                return null;
            }

            var body = eventModel.Content;
            var doc = new HtmlDocument();
            doc.LoadHtml(body);

            var number = doc.DocumentNode.SelectSingleNode("//a[contains(@href, 'tel')]");
            if (number == null || string.IsNullOrEmpty(number.InnerText))
            {
                return null;
            }

            const string telToken = "&#43;";
            return number.InnerText.Replace(telToken, string.Empty);
        }

        private string GetTeamsMeetingLinkFromMeeting(EventModel eventModel)
        {
            if (string.IsNullOrEmpty(eventModel.Content))
            {
                return null;
            }

            var body = eventModel.Content;
            var doc = new HtmlDocument();
            doc.LoadHtml(body);

            var meetingLink = doc.DocumentNode.SelectSingleNode("//a[contains(string(), 'Join')][contains(string(), 'Microsoft')][contains(string(), 'Teams')][contains(string(), 'Meeting')]");
            if (meetingLink != null)
            {
                return meetingLink.GetAttributeValue("href", null);
            }

            return null;
        }

        private bool IsValidJoinTime(TimeZoneInfo userTimeZone, EventModel e)
        {
            var startTime = TimeZoneInfo.ConvertTime(e.StartTime, TimeZoneInfo.Utc, userTimeZone);
            var endTime = TimeZoneInfo.ConvertTime(e.EndTime, TimeZoneInfo.Utc, userTimeZone);
            var nowTime = TimeZoneInfo.ConvertTime(DateTime.UtcNow, TimeZoneInfo.Utc, userTimeZone);

            if (endTime >= nowTime || nowTime.AddHours(1) >= startTime)
            {
                return true;
            }

            return false;
        }

        private async Task<DialogTurnResult> GetEventsAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.GetEvents, sc.Options);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> GetEventsPromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                if (state.ShowMeetingInfo.FocusedEvents.Any())
                {
                    return await sc.EndDialogAsync();
                }
                else if (state.ShowMeetingInfo.ShowingMeetings.Any())
                {
                    return await sc.NextAsync();
                }
                else
                {
                    sc.Context.TurnState.TryGetValue(StateProperties.APITokenKey, out var token);
                    var calendarService = ServiceManager.InitCalendarService(token as string, state.EventSource);
                    return await sc.PromptAsync(Actions.GetEventPrompt, new GetEventOptions(calendarService, state.GetUserTimeZone())
                    {
                        Prompt = TemplateManager.GenerateActivityForLocale(JoinEventResponses.NoMeetingToConnect) as Activity,
                        RetryPrompt = TemplateManager.GenerateActivityForLocale(JoinEventResponses.NoMeetingToConnect) as Activity,
                        MaxReprompt = CalendarCommonUtil.MaxRepromptCount
                    }, cancellationToken);
                }
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptionsAsync(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CheckValidAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                var validEvents = new List<EventModel>();
                foreach (var item in state.ShowMeetingInfo.ShowingMeetings)
                {
                    if (IsValidJoinTime(state.GetUserTimeZone(), item) && (GetDialInNumberFromMeeting(item) != null || item.OnlineMeetingUrl != null || GetTeamsMeetingLinkFromMeeting(item) != null))
                    {
                        validEvents.Add(item);
                    }
                }

                state.ShowMeetingInfo.ShowingMeetings = validEvents;
                if (validEvents.Any())
                {
                    return await sc.EndDialogAsync();
                }
                else
                {
                    return await sc.BeginDialogAsync(Actions.GetEvents, sc.Options);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> ConfirmNumberAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);

                var selectedEvent = state.ShowMeetingInfo.FocusedEvents.First();
                var phoneNumber = GetDialInNumberFromMeeting(selectedEvent);
                var meetingLink = selectedEvent.OnlineMeetingUrl ?? GetTeamsMeetingLinkFromMeeting(selectedEvent);

                var responseParams = new
                {
                    PhoneNumber = phoneNumber,
                    MeetingLink = meetingLink
                };
                var responseName = phoneNumber == null ? JoinEventResponses.ConfirmMeetingLink : JoinEventResponses.ConfirmPhoneNumber;

                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions()
                {
                    Prompt = TemplateManager.GenerateActivityForLocale(responseName, responseParams) as Activity,
                });
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterConfirmNumberAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context);
                var options = (CalendarSkillDialogOptions)sc.Options;
                var status = false;
                if (sc.Result is bool)
                {
                    if ((bool)sc.Result)
                    {
                        var selectedEvent = state.ShowMeetingInfo.FocusedEvents.First();
                        var activity = TemplateManager.GenerateActivityForLocale(JoinEventResponses.JoinMeeting);
                        await sc.Context.SendActivityAsync(activity);
                        var replyEvent = sc.Context.Activity.CreateReply();
                        replyEvent.Type = ActivityTypes.Event;
                        replyEvent.Name = "OpenDefaultApp";
                        var eventJoinLink = new OpenDefaultApp
                        {
                            MeetingUri = selectedEvent.OnlineMeetingUrl ?? GetTeamsMeetingLinkFromMeeting(selectedEvent),
                            TelephoneUri = "tel:" + GetDialInNumberFromMeeting(selectedEvent)
                        };
                        replyEvent.Value = JsonConvert.SerializeObject(eventJoinLink);
                        await sc.Context.SendActivityAsync(replyEvent, cancellationToken);
                        status = true;
                    }
                    else
                    {
                        var activity = TemplateManager.GenerateActivityForLocale(JoinEventResponses.NotJoinMeeting);
                        await sc.Context.SendActivityAsync(activity);
                    }
                }

                if (options.SubFlowMode)
                {
                    state.ShowMeetingInfo.ShowingMeetings.Clear();
                }

                if (state.IsAction)
                {
                    return await sc.EndDialogAsync(new ActionResult() { ActionSuccess = status });
                }

                return await sc.EndDialogAsync();
            }
            catch (SkillException ex)
            {
                await HandleDialogExceptionsAsync(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}