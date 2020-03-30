// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.ActionInfos;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Prompts.Options;
using CalendarSkill.Responses.ChangeEventStatus;
using CalendarSkill.Utilities;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;

namespace CalendarSkill.Dialogs
{
    public class ChangeEventStatusDialog : CalendarSkillDialogBase
    {
        public ChangeEventStatusDialog(
            IServiceProvider serviceProvider)
            : base(nameof(ChangeEventStatusDialog), serviceProvider)
        {
            var changeEventStatus = new WaterfallStep[]
            {
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                CheckFocusedEventAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                ConfirmBeforeActionAsync,
                AfterConfirmBeforeActionAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                ChangeEventStatusAsync
            };

            var findEvent = new WaterfallStep[]
            {
                SearchEventsWithEntitiesAsync,
                GetEventsAsync,
                AfterGetEventsPromptAsync,
                AddConflictFlagAsync,
                ChooseEventAsync
            };

            var chooseEvent = new WaterfallStep[]
            {
                ChooseEventPromptAsync,
                AfterChooseEventAsync
            };

            AddDialog(new WaterfallDialog(Actions.ChangeEventStatus, changeEventStatus) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.FindEvent, findEvent) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.ChooseEvent, chooseEvent) { TelemetryClient = TelemetryClient });

            // Set starting dialog for component
            InitialDialogId = Actions.ChangeEventStatus;
        }

        private async Task<DialogTurnResult> ConfirmBeforeActionAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var options = (ChangeEventStatusDialogOptions)sc.Options;

                var deleteEvent = state.ShowMeetingInfo.FocusedEvents[0];
                string replyResponse;
                string retryResponse;
                if (options.NewEventStatus == EventStatus.Cancelled)
                {
                    replyResponse = ChangeEventStatusResponses.ConfirmDelete;
                    retryResponse = ChangeEventStatusResponses.ConfirmDeleteFailed;
                }
                else
                {
                    replyResponse = ChangeEventStatusResponses.ConfirmAccept;
                    retryResponse = ChangeEventStatusResponses.ConfirmAcceptFailed;
                }

                var startTime = TimeConverter.ConvertUtcToUserTime(deleteEvent.StartTime, state.GetUserTimeZone());

                var responseParams = new
                {
                    Time = startTime.ToString(CommonStrings.DisplayTime),
                    Title = deleteEvent.Title
                };

                var replyMessage = await GetDetailMeetingResponseAsync(sc, deleteEvent, replyResponse, responseParams);
                var retryMessage = TemplateManager.GenerateActivityForLocale(retryResponse, responseParams) as Activity;
                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = replyMessage,
                    RetryPrompt = retryMessage,
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterConfirmBeforeActionAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var options = (ChangeEventStatusDialogOptions)sc.Options;

                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
                else
                {
                    if (options.SubFlowMode)
                    {
                        state.MeetingInfo.ClearTimes();
                        state.MeetingInfo.ClearTitle();
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

        private async Task<DialogTurnResult> ChangeEventStatusAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var options = (ChangeEventStatusDialogOptions)sc.Options;
                sc.Context.TurnState.TryGetValue(StateProperties.APITokenKey, out var token);

                var calendarService = ServiceManager.InitCalendarService(token as string, state.EventSource);
                var deleteEvent = state.ShowMeetingInfo.FocusedEvents[0];
                if (options.NewEventStatus == EventStatus.Cancelled)
                {
                    if (deleteEvent.IsOrganizer)
                    {
                        await calendarService.DeleteEventByIdAsync(deleteEvent.Id);
                    }
                    else
                    {
                        await calendarService.DeclineEventByIdAsync(deleteEvent.Id);
                    }

                    var activity = TemplateManager.GenerateActivityForLocale(ChangeEventStatusResponses.EventDeleted);
                    await sc.Context.SendActivityAsync(activity, cancellationToken);
                }
                else
                {
                    await calendarService.AcceptEventByIdAsync(deleteEvent.Id);

                    var activity = TemplateManager.GenerateActivityForLocale(ChangeEventStatusResponses.EventAccepted);
                    await sc.Context.SendActivityAsync(activity, cancellationToken);
                }

                if (options.SubFlowMode)
                {
                    state.MeetingInfo.ClearTimes();
                    state.MeetingInfo.ClearTitle();
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

        private async Task<DialogTurnResult> GetEventsAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var options = (ChangeEventStatusDialogOptions)sc.Options;

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
                    if (options.NewEventStatus == EventStatus.Cancelled)
                    {
                        return await sc.PromptAsync(Actions.GetEventPrompt, new GetEventOptions(calendarService, state.GetUserTimeZone())
                        {
                            Prompt = TemplateManager.GenerateActivityForLocale(ChangeEventStatusResponses.NoDeleteStartTime) as Activity,
                            RetryPrompt = TemplateManager.GenerateActivityForLocale(ChangeEventStatusResponses.EventWithStartTimeNotFound) as Activity,
                            MaxReprompt = CalendarCommonUtil.MaxRepromptCount
                        }, cancellationToken);
                    }
                    else
                    {
                        return await sc.PromptAsync(Actions.GetEventPrompt, new GetEventOptions(calendarService, state.GetUserTimeZone())
                        {
                            Prompt = TemplateManager.GenerateActivityForLocale(ChangeEventStatusResponses.NoAcceptStartTime) as Activity,
                            RetryPrompt = TemplateManager.GenerateActivityForLocale(ChangeEventStatusResponses.EventWithStartTimeNotFound) as Activity,
                            MaxReprompt = CalendarCommonUtil.MaxRepromptCount
                        }, cancellationToken);
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
    }
}