// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.ActionInfos;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Prompts;
using CalendarSkill.Prompts.Options;
using CalendarSkill.Responses.CreateEvent;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Resources;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Graph;
using static CalendarSkill.Models.CreateEventStateModel;

namespace CalendarSkill.Dialogs
{
    public class CreateEventDialog : CalendarSkillDialogBase
    {
        public CreateEventDialog(
            IServiceProvider serviceProvider)
            : base(nameof(CreateEventDialog), serviceProvider)
        {
            var createEvent = new WaterfallStep[]
            {
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                CollectAttendeesAsync,
                CollectTitleAsync,
                CollectContentAsync,
                CollectStartDatetAsync,
                CollectStartTimeAsync,
                CollectDurationAsync,
                CollectLocationAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                ShowEventInfoAsync,
                ConfirmBeforeCreatePromptAsync,
                AfterConfirmBeforeCreatePromptAsync,
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                CreateEventAsync,
            };

            var collectTitle = new WaterfallStep[]
            {
                CollectTitlePromptAsync,
                AfterCollectTitlePromptAsync
            };

            var collectContent = new WaterfallStep[]
            {
                CollectContentPromptAsync,
                AfterCollectContentPromptAsync
            };

            var updateStartDate = new WaterfallStep[]
            {
                UpdateStartDateForCreateAsync,
                AfterUpdateStartDateForCreateAsync,
            };

            var updateStartTime = new WaterfallStep[]
            {
                UpdateStartTimeForCreateAsync,
                AfterUpdateStartTimeForCreateAsync,
            };

            var collectLocation = new WaterfallStep[]
            {
                CollectMeetingRoomPromptAsync,
                AfterCollectMeetingRoomPromptAsync,
                CollectLocationPromptAsync,
                AfterCollectLocationPromptAsync
            };

            var updateDuration = new WaterfallStep[]
            {
                UpdateDurationForCreateAsync,
                AfterUpdateDurationForCreateAsync,
            };

            var getRecreateInfo = new WaterfallStep[]
            {
                GetRecreateInfoAsync,
                AfterGetRecreateInfoAsync,
            };

            var showRestParticipants = new WaterfallStep[]
            {
                ShowRestParticipantsPromptAsync,
                AfterShowRestParticipantsPromptAsync,
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.CreateEvent, createEvent) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectTitle, collectTitle) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectContent, collectContent) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.CollectLocation, collectLocation) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateStartDateForCreate, updateStartDate) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateStartTimeForCreate, updateStartTime) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.UpdateDurationForCreate, updateDuration) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.GetRecreateInfo, getRecreateInfo) { TelemetryClient = TelemetryClient });
            AddDialog(new WaterfallDialog(Actions.ShowRestParticipants, showRestParticipants) { TelemetryClient = TelemetryClient });
            AddDialog(new DatePrompt(Actions.DatePromptForCreate));
            AddDialog(new TimePrompt(Actions.TimePromptForCreate));
            AddDialog(new DurationPrompt(Actions.DurationPromptForCreate));
            AddDialog(new GetRecreateInfoPrompt(Actions.GetRecreateInfoPrompt));
            AddDialog(serviceProvider.GetService<FindContactDialog>() ?? throw new ArgumentNullException(nameof(FindContactDialog)));

            // Set starting dialog for component
            InitialDialogId = Actions.CreateEvent;
        }

        // Create Event waterfall steps
        private async Task<DialogTurnResult> CollectTitleAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.CollectTitle, sc.Options, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectTitlePromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isTitleSkipByDefault = false;
                isTitleSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventTitle")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (state.MeetingInfo.RecreateState == RecreateEventState.Subject)
                {
                    var prompt = TemplateManager.GenerateActivityForLocale(CreateEventResponses.NoTitleShort) as Activity;
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt }, cancellationToken);
                }
                else if (state.MeetingInfo.CreateHasDetail && isTitleSkipByDefault.GetValueOrDefault())
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
                else if (string.IsNullOrEmpty(state.MeetingInfo.Title))
                {
                    if (state.MeetingInfo.ContactInfor.Contacts.Count == 0 || state.MeetingInfo.ContactInfor.Contacts == null)
                    {
                        state.Clear();
                        return await sc.EndDialogAsync(true, cancellationToken);
                    }

                    var userNameString = state.MeetingInfo.ContactInfor.Contacts.ToSpeechString(CommonStrings.And, li => $"{li.DisplayName ?? li.Address}");
                    var data = new { UserName = userNameString };
                    var prompt = TemplateManager.GenerateActivityForLocale(CreateEventResponses.NoTitle, data) as Activity;

                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt }, cancellationToken);
                }
                else
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterCollectTitlePromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isTitleSkipByDefault = false;
                isTitleSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventTitle")?.IsSkipByDefault;
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (sc.Result != null || (state.MeetingInfo.CreateHasDetail && isTitleSkipByDefault.GetValueOrDefault()) || state.MeetingInfo.RecreateState == RecreateEventState.Subject)
                {
                    if (string.IsNullOrEmpty(state.MeetingInfo.Title))
                    {
                        if (state.MeetingInfo.CreateHasDetail && isTitleSkipByDefault.GetValueOrDefault() && state.MeetingInfo.RecreateState != RecreateEventState.Subject)
                        {
                            state.MeetingInfo.Title = CreateEventWhiteList.GetDefaultTitle();
                        }
                        else
                        {
                            sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                            var title = content != null ? content.ToString() : sc.Context.Activity.Text;
                            if (CreateEventWhiteList.IsSkip(title))
                            {
                                state.MeetingInfo.Title = CreateEventWhiteList.GetDefaultTitle();
                            }
                            else
                            {
                                state.MeetingInfo.Title = title;
                            }
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

        private async Task<DialogTurnResult> CollectContentAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.BeginDialogAsync(Actions.CollectContent, sc.Options, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectContentPromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isContentSkipByDefault = false;
                isContentSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventContent")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (string.IsNullOrEmpty(state.MeetingInfo.Content) && (!(state.MeetingInfo.CreateHasDetail && isContentSkipByDefault.GetValueOrDefault()) || state.MeetingInfo.RecreateState == RecreateEventState.Content))
                {
                    var prompt = TemplateManager.GenerateActivityForLocale(CreateEventResponses.NoContent) as Activity;
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt }, cancellationToken);
                }
                else
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterCollectContentPromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isContentSkipByDefault = false;
                isContentSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventContent")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (sc.Result != null && (!(state.MeetingInfo.CreateHasDetail && isContentSkipByDefault.GetValueOrDefault()) || state.MeetingInfo.RecreateState == RecreateEventState.Content))
                {
                    if (string.IsNullOrEmpty(state.MeetingInfo.Content))
                    {
                        sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                        var merged_content = content != null ? content.ToString() : sc.Context.Activity.Text;
                        if (!CreateEventWhiteList.IsSkip(merged_content))
                        {
                            state.MeetingInfo.Content = merged_content;
                        }
                    }
                }
                else if (state.MeetingInfo.CreateHasDetail && isContentSkipByDefault.GetValueOrDefault())
                {
                    state.MeetingInfo.Content = CalendarCommonStrings.DefaultContent;
                }

                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectAttendeesAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                if (state.MeetingInfo.ContactInfor.Contacts.Count == 0 || state.MeetingInfo.RecreateState == RecreateEventState.Participants)
                {
                    return await sc.BeginDialogAsync(nameof(FindContactDialog), options: new FindContactDialogOptions(sc.Options), cancellationToken: cancellationToken);
                }
                else
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectLocationAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfo.Location == null && state.MeetingInfo.MeetingRoom == null)
                {
                    return await sc.BeginDialogAsync(Actions.CollectLocation, sc.Options, cancellationToken);
                }
                else
                {
                    state.MeetingInfo.Location = state.MeetingInfo.Location ?? state.MeetingInfo.MeetingRoom.DisplayName;
                    return await sc.NextAsync(cancellationToken: cancellationToken);
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

        private async Task<DialogTurnResult> CollectMeetingRoomPromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfo.RecreateState == RecreateEventState.MeetingRoom || string.IsNullOrEmpty(Settings.AzureSearch?.SearchServiceName))
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
                else
                {
                    var prompt = TemplateManager.GenerateActivityForLocale(CreateEventResponses.NoMeetingRoom);
                    return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions { Prompt = prompt }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterCollectMeetingRoomPromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfo.RecreateState == RecreateEventState.MeetingRoom || (sc.Result != null && (bool)sc.Result == true))
                {
                    return await sc.BeginDialogAsync(nameof(FindMeetingRoomDialog), options: sc.Options, cancellationToken: cancellationToken);
                }
                else
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CollectLocationPromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isLocationSkipByDefault = false;
                isLocationSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventLocation")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfo.MeetingRoom != null)
                {
                    state.MeetingInfo.Location = state.MeetingInfo.MeetingRoom.DisplayName;
                    return await sc.EndDialogAsync(cancellationToken: cancellationToken);
                }
                else if (state.MeetingInfo.Location == null && (!(state.MeetingInfo.CreateHasDetail && isLocationSkipByDefault.GetValueOrDefault()) || state.MeetingInfo.RecreateState == RecreateEventState.Location))
                {
                    var prompt = TemplateManager.GenerateActivityForLocale(CreateEventResponses.NoLocation) as Activity;
                    return await sc.PromptAsync(Actions.Prompt, new PromptOptions { Prompt = prompt }, cancellationToken);
                }
                else
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterCollectLocationPromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                bool? isLocationSkipByDefault = false;
                isLocationSkipByDefault = Settings.DefaultValue?.CreateMeeting?.First(item => item.Name == "EventLocation")?.IsSkipByDefault;

                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (state.MeetingInfo.Location == null && sc.Result != null && (!(state.MeetingInfo.CreateHasDetail && isLocationSkipByDefault.GetValueOrDefault()) || state.MeetingInfo.RecreateState == RecreateEventState.Location))
                {
                    sc.Context.Activity.Properties.TryGetValue("OriginText", out var content);
                    var luisResult = sc.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);

                    var userInput = content != null ? content.ToString() : sc.Context.Activity.Text;
                    var topIntent = luisResult?.TopIntent().intent.ToString();

                    var promptRecognizerResult = ConfirmRecognizerHelper.ConfirmYesOrNo(userInput, sc.Context.Activity.Locale);

                    // Enable the user to skip providing the location if they say something matching the Cancel intent, say something matching the ConfirmNo recognizer or something matching the NoLocation intent
                    if (CreateEventWhiteList.IsSkip(userInput))
                    {
                        state.MeetingInfo.Location = string.Empty;
                    }
                    else
                    {
                        state.MeetingInfo.Location = userInput;
                    }
                }
                else if (state.MeetingInfo.CreateHasDetail && isLocationSkipByDefault.GetValueOrDefault())
                {
                    state.MeetingInfo.Location = CalendarCommonStrings.DefaultLocation;
                }

                return await sc.EndDialogAsync(cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> ShowEventInfoAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // show event information before create
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);

                var source = state.EventSource;
                var newEvent = new EventModel(source)
                {
                    Title = state.MeetingInfo.Title,
                    Content = state.MeetingInfo.Content,
                    Attendees = state.MeetingInfo.ContactInfor.Contacts,
                    StartTime = state.MeetingInfo.StartDateTime.Value,
                    EndTime = state.MeetingInfo.EndDateTime.Value,
                    TimeZone = TimeZoneInfo.Utc,
                    Location = state.MeetingInfo.Location,
                    ContentPreview = state.MeetingInfo.Content
                };

                var attendeeConfirmTextString = string.Empty;
                if (state.MeetingInfo.ContactInfor.Contacts.Count > 0)
                {
                    var attendeeConfirmResponse = TemplateManager.GenerateActivityForLocale(CreateEventResponses.ConfirmCreateAttendees, new
                    {
                        Attendees = DisplayHelper.ToDisplayParticipantsStringSummary(state.MeetingInfo.ContactInfor.Contacts, 5)
                    });
                    attendeeConfirmTextString = attendeeConfirmResponse.Text;
                }

                var subjectConfirmString = string.Empty;
                if (!string.IsNullOrEmpty(state.MeetingInfo.Title))
                {
                    var subjectConfirmResponse = TemplateManager.GenerateActivityForLocale(CreateEventResponses.ConfirmCreateSubject, new
                    {
                        Subject = string.IsNullOrEmpty(state.MeetingInfo.Title) ? CalendarCommonStrings.Empty : state.MeetingInfo.Title
                    });
                    subjectConfirmString = subjectConfirmResponse.Text;
                }

                var locationConfirmString = string.Empty;
                if (!string.IsNullOrEmpty(state.MeetingInfo.Location))
                {
                    var subjectConfirmResponse = TemplateManager.GenerateActivityForLocale(CreateEventResponses.ConfirmCreateLocation, new
                    {
                        Location = string.IsNullOrEmpty(state.MeetingInfo.Location) ? CalendarCommonStrings.Empty : state.MeetingInfo.Location
                    });
                    locationConfirmString = subjectConfirmResponse.Text;
                }

                var contentConfirmString = string.Empty;
                if (!string.IsNullOrEmpty(state.MeetingInfo.Content))
                {
                    var contentConfirmResponse = TemplateManager.GenerateActivityForLocale(CreateEventResponses.ConfirmCreateContent, new
                    {
                        Content = string.IsNullOrEmpty(state.MeetingInfo.Content) ? CalendarCommonStrings.Empty : state.MeetingInfo.Content
                    });
                    contentConfirmString = contentConfirmResponse.Text;
                }

                var startDateTimeInUserTimeZone = TimeConverter.ConvertUtcToUserTime(state.MeetingInfo.StartDateTime.Value, state.GetUserTimeZone());
                var endDateTimeInUserTimeZone = TimeConverter.ConvertUtcToUserTime(state.MeetingInfo.EndDateTime.Value, state.GetUserTimeZone());
                var tokens = new
                {
                    AttendeesConfirm = attendeeConfirmTextString,
                    Date = startDateTimeInUserTimeZone.ToSpeechDateString(false),
                    Time = startDateTimeInUserTimeZone.ToSpeechTimeString(false),
                    EndTime = endDateTimeInUserTimeZone.ToSpeechTimeString(false),
                    SubjectConfirm = subjectConfirmString,
                    LocationConfirm = locationConfirmString,
                    ContentConfirm = contentConfirmString
                };

                var prompt = await GetDetailMeetingResponseAsync(sc, newEvent, CreateEventResponses.ConfirmCreate, tokens, cancellationToken);

                await sc.Context.SendActivityAsync(prompt, cancellationToken);

                // show at most 5 user names, ask user show rest users
                if (state.MeetingInfo.ContactInfor.Contacts.Count > 5)
                {
                    return await sc.BeginDialogAsync(Actions.ShowRestParticipants, cancellationToken: cancellationToken);
                }
                else
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> ConfirmBeforeCreatePromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = TemplateManager.GenerateActivityForLocale(CreateEventResponses.ConfirmCreatePrompt) as Activity,
                    RetryPrompt = TemplateManager.GenerateActivityForLocale(CreateEventResponses.ConfirmCreateFailed) as Activity
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterConfirmBeforeCreatePromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    return await sc.NextAsync(cancellationToken: cancellationToken);
                }
                else
                {
                    // if user not create, ask if user want to change any field
                    return await sc.ReplaceDialogAsync(Actions.GetRecreateInfo, options: sc.Options, cancellationToken: cancellationToken);
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> CreateEventAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var source = state.EventSource;

                if (state.MeetingInfo.MeetingRoom != null)
                {
                    state.MeetingInfo.ContactInfor.Contacts.Add(new EventModel.Attendee
                    {
                        DisplayName = state.MeetingInfo.MeetingRoom.DisplayName,
                        Address = state.MeetingInfo.MeetingRoom.EmailAddress,
                        AttendeeType = AttendeeType.Resource
                    });
                }

                var userTimezone = state.GetUserTimeZone();
                var newEvent = new EventModel(source)
                {
                    Title = state.MeetingInfo.Title,
                    Content = state.MeetingInfo.Content,
                    Attendees = state.MeetingInfo.ContactInfor.Contacts,
                    StartTime = TimeConverter.ConvertUtcToUserTime((DateTime)state.MeetingInfo.StartDateTime, userTimezone),
                    EndTime = TimeConverter.ConvertUtcToUserTime((DateTime)state.MeetingInfo.EndDateTime, userTimezone),
                    TimeZone = userTimezone,
                    Location = state.MeetingInfo.MeetingRoom == null ? state.MeetingInfo.Location : null,
                    IsOnlineMeeting = true
                };

                var status = false;
                sc.Context.TurnState.TryGetValue(StateProperties.APITokenKey, out var token);
                var calendarService = ServiceManager.InitCalendarService(token as string, state.EventSource);
                var createdEvent = await calendarService.CreateEventAsync(newEvent);
                if (createdEvent != null)
                {
                    var activity = TemplateManager.GenerateActivityForLocale(CreateEventResponses.MeetingBooked);
                    await sc.Context.SendActivityAsync(activity, cancellationToken);
                    status = true;
                }
                else
                {
                    var activity = TemplateManager.GenerateActivityForLocale(CreateEventResponses.EventCreationFailed);
                    await sc.Context.SendActivityAsync(activity, cancellationToken);
                    status = false;
                }

                if (state.IsAction)
                {
                    return await sc.EndDialogAsync(new ActionResult() { ActionSuccess = status }, cancellationToken);
                }

                return await sc.EndDialogAsync(true, cancellationToken);
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

        private async Task<DialogTurnResult> GetRecreateInfoAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.PromptAsync(Actions.GetRecreateInfoPrompt, new CalendarPromptOptions
                {
                    Prompt = TemplateManager.GenerateActivityForLocale(CreateEventResponses.GetRecreateInfo) as Activity,
                    RetryPrompt = TemplateManager.GenerateActivityForLocale(CreateEventResponses.GetRecreateInfoRetry) as Activity,
                    MaxReprompt = CalendarCommonUtil.MaxRepromptCount
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);
                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterGetRecreateInfoAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                if (sc.Result != null)
                {
                    var recreateState = sc.Result as RecreateEventState?;
                    switch (recreateState.Value)
                    {
                        case RecreateEventState.Cancel:
                            state.Clear();
                            return await sc.EndDialogAsync(true, cancellationToken);
                        case RecreateEventState.Time:
                            state.MeetingInfo.ClearTimesForRecreate();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        case RecreateEventState.Duration:
                            state.MeetingInfo.ClearEndTimesAndDurationForRecreate();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        case RecreateEventState.Location:
                            state.MeetingInfo.ClearLocationForRecreate();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        case RecreateEventState.MeetingRoom:
                            state.MeetingInfo.ClearMeetingRoomForRecreate();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        case RecreateEventState.Participants:
                            state.MeetingInfo.ClearParticipantsForRecreate();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        case RecreateEventState.Subject:
                            state.MeetingInfo.ClearSubjectForRecreate();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        case RecreateEventState.Content:
                            state.MeetingInfo.ClearContentForRecreate();
                            return await sc.ReplaceDialogAsync(Actions.CreateEvent, options: sc.Options, cancellationToken: cancellationToken);
                        default:
                            // should not go to this part. place an error handling for save.
                            await HandleDialogExceptionsAsync(sc, new Exception("Get unexpect state in recreate."), cancellationToken);
                            return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
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

        private async Task<DialogTurnResult> ShowRestParticipantsPromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                return await sc.PromptAsync(Actions.TakeFurtherAction, new PromptOptions
                {
                    Prompt = TemplateManager.GenerateActivityForLocale(CreateEventResponses.ShowRestParticipantsPrompt) as Activity,
                    RetryPrompt = TemplateManager.GenerateActivityForLocale(CreateEventResponses.ShowRestParticipantsPrompt) as Activity,
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }

        private async Task<DialogTurnResult> AfterShowRestParticipantsPromptAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await Accessor.GetAsync(sc.Context, cancellationToken: cancellationToken);
                var confirmResult = (bool)sc.Result;
                if (confirmResult)
                {
                    await sc.Context.SendActivityAsync(state.MeetingInfo.ContactInfor.Contacts.GetRange(5, state.MeetingInfo.ContactInfor.Contacts.Count - 5).ToSpeechString(CommonStrings.And, li => li.DisplayName ?? li.Address), cancellationToken: cancellationToken);
                }

                return await sc.EndDialogAsync(true, cancellationToken);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptionsAsync(sc, ex, cancellationToken);

                return new DialogTurnResult(DialogTurnStatus.Cancelled, CommonUtil.DialogTurnResultCancelAllDialogs);
            }
        }
    }
}