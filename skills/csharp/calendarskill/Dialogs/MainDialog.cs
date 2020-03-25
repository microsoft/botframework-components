// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using CalendarSkill.Models;
using CalendarSkill.Models.ActionInfos;
using CalendarSkill.Models.DialogOptions;
using CalendarSkill.Responses.Main;
using CalendarSkill.Responses.Shared;
using CalendarSkill.Services;
using CalendarSkill.Utilities;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using SkillServiceLibrary.Utilities;

namespace CalendarSkill.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private LocaleTemplateManager _templateManager;
        private IStatePropertyAccessor<CalendarSkillState> _stateAccessor;
        private Dialog _createEventDialog;
        private Dialog _changeEventStatusDialog;
        private Dialog _timeRemainingDialog;
        private Dialog _showEventsDialog;
        private Dialog _updateEventDialog;
        private Dialog _joinEventDialog;
        private Dialog _upcomingEventDialog;
        private Dialog _checkPersonAvailableDialog;
        private Dialog _findMeetingRoomDialog;
        private Dialog _updateMeetingRoomDialog;
        private Dialog _bookMeetingRoomDialog;

        public MainDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog))
        {
            _settings = serviceProvider.GetService<BotSettings>();
            _services = serviceProvider.GetService<BotServices>();
            _templateManager = serviceProvider.GetService<LocaleTemplateManager>();
            TelemetryClient = telemetryClient;

            // Create conversation state properties
            var conversationState = serviceProvider.GetService<ConversationState>();
            _stateAccessor = conversationState.CreateProperty<CalendarSkillState>(nameof(CalendarSkillState));

            var steps = new WaterfallStep[]
            {
                IntroStepAsync,
                RouteStepAsync,
                FinalStepAsync,
            };

            AddDialog(new WaterfallDialog(nameof(MainDialog), steps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            InitialDialogId = nameof(MainDialog);

            // Register dialogs
            _createEventDialog = serviceProvider.GetService<CreateEventDialog>() ?? throw new ArgumentNullException(nameof(CreateEventDialog));
            _changeEventStatusDialog = serviceProvider.GetService<ChangeEventStatusDialog>() ?? throw new ArgumentNullException(nameof(ChangeEventStatusDialog));
            _timeRemainingDialog = serviceProvider.GetService<TimeRemainingDialog>() ?? throw new ArgumentNullException(nameof(TimeRemainingDialog));
            _showEventsDialog = serviceProvider.GetService<ShowEventsDialog>() ?? throw new ArgumentNullException(nameof(ShowEventsDialog));
            _updateEventDialog = serviceProvider.GetService<UpdateEventDialog>() ?? throw new ArgumentNullException(nameof(UpdateEventDialog));
            _joinEventDialog = serviceProvider.GetService<JoinEventDialog>() ?? throw new ArgumentNullException(nameof(JoinEventDialog));
            _upcomingEventDialog = serviceProvider.GetService<UpcomingEventDialog>() ?? throw new ArgumentNullException(nameof(UpcomingEventDialog));
            _checkPersonAvailableDialog = serviceProvider.GetService<CheckPersonAvailableDialog>() ?? throw new ArgumentNullException(nameof(CheckPersonAvailableDialog));
            _findMeetingRoomDialog = serviceProvider.GetService<FindMeetingRoomDialog>() ?? throw new ArgumentNullException(nameof(FindMeetingRoomDialog));
            _updateMeetingRoomDialog = serviceProvider.GetService<UpdateMeetingRoomDialog>() ?? throw new ArgumentNullException(nameof(UpdateMeetingRoomDialog));
            _updateEventDialog = serviceProvider.GetService<UpdateEventDialog>() ?? throw new ArgumentNullException(nameof(UpdateEventDialog));
            _bookMeetingRoomDialog = serviceProvider.GetService<BookMeetingRoomDialog>() ?? throw new ArgumentNullException(nameof(BookMeetingRoomDialog));
            AddDialog(_createEventDialog);
            AddDialog(_changeEventStatusDialog);
            AddDialog(_timeRemainingDialog);
            AddDialog(_showEventsDialog);
            AddDialog(_updateEventDialog);
            AddDialog(_joinEventDialog);
            AddDialog(_upcomingEventDialog);
            AddDialog(_checkPersonAvailableDialog);
            AddDialog(_findMeetingRoomDialog);
            AddDialog(_updateMeetingRoomDialog);
            AddDialog(_bookMeetingRoomDialog);
        }

        // Runs when the dialog is started.
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(innerDc.Context.Activity.Text))
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                var luisResult = innerDc.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
                if (luisResult == null)
                {
                    // Run LUIS recognition on Skill model and store result in turn state.
                    localizedServices.LuisServices.TryGetValue("Calendar", out var skillLuisService);
                    if (skillLuisService != null)
                    {
                        var skillResult = await skillLuisService.RecognizeAsync<CalendarLuis>(innerDc.Context, cancellationToken);
                        innerDc.Context.TurnState[StateProperties.CalendarLuisResultKey] = skillResult;
                    }
                    else
                    {
                        throw new Exception("The skill LUIS Model could not be found in your Bot Services configuration.");
                    }
                }

                // Run LUIS recognition on General model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("General", out var generalLuisService);
                if (generalLuisService != null)
                {
                    var generalResult = await generalLuisService.RecognizeAsync<General>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState[StateProperties.GeneralLuisResultKey] = generalResult;
                }
                else
                {
                    throw new Exception("The general LUIS Model could not be found in your Bot Services configuration.");
                }

                // Check for any interruptions
                var interrupted = await InterruptDialogAsync(innerDc, cancellationToken);

                if (interrupted != null)
                {
                    // If dialog was interrupted, return interrupted result
                    return interrupted;
                }
            }

            return await base.OnBeginDialogAsync(innerDc, options, cancellationToken);
        }

        // Runs on every turn of the conversation.
        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext innerDc, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(innerDc.Context.Activity.Text))
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                var luisResult = innerDc.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
                if (luisResult == null)
                {
                    // Run LUIS recognition on Skill model and store result in turn state.
                    localizedServices.LuisServices.TryGetValue("Calendar", out var skillLuisService);
                    if (skillLuisService != null)
                    {
                        var skillResult = await skillLuisService.RecognizeAsync<CalendarLuis>(innerDc.Context, cancellationToken);
                        innerDc.Context.TurnState[StateProperties.CalendarLuisResultKey] = skillResult;
                    }
                    else
                    {
                        throw new Exception("The skill LUIS Model could not be found in your Bot Services configuration.");
                    }
                }

                // Run LUIS recognition on General model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("General", out var generalLuisService);
                if (generalLuisService != null)
                {
                    var generalResult = await generalLuisService.RecognizeAsync<General>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState[StateProperties.GeneralLuisResultKey] = generalResult;
                }
                else
                {
                    throw new Exception("The general LUIS Model could not be found in your Bot Services configuration.");
                }

                // Check for any interruptions
                var interrupted = await InterruptDialogAsync(innerDc, cancellationToken);

                if (interrupted != null)
                {
                    // If dialog was interrupted, return interrupted result
                    return interrupted;
                }
            }

            return await base.OnContinueDialogAsync(innerDc, cancellationToken);
        }

        // Runs on every turn of the conversation to check if the conversation should be interrupted.
        protected async Task<DialogTurnResult> InterruptDialogAsync(DialogContext innerDc, CancellationToken cancellationToken)
        {
            DialogTurnResult interrupted = null;
            var activity = innerDc.Context.Activity;

            if (activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(activity.Text))
            {
                // Get connected LUIS result from turn state.
                var state = await _stateAccessor.GetAsync(innerDc.Context, () => new CalendarSkillState());
                var generalResult = innerDc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                (var generalIntent, var generalScore) = generalResult.TopIntent();

                if (generalScore > 0.5)
                {
                    switch (generalIntent)
                    {
                        case General.Intent.Cancel:
                            {
                                state.Clear();
                                await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale(CalendarMainResponses.CancelMessage));
                                await innerDc.CancelAllDialogsAsync();
                                if (innerDc.Context.IsSkill())
                                {
                                    interrupted = await innerDc.EndDialogAsync(state.IsAction ? new ActionResult { ActionSuccess = false } : null, cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    interrupted = await innerDc.BeginDialogAsync(InitialDialogId, cancellationToken: cancellationToken);
                                }

                                interrupted = EndOfTurn;
                                break;
                            }

                        case General.Intent.Help:
                            {
                                await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale(CalendarMainResponses.HelpMessage));
                                await innerDc.RepromptDialogAsync();
                                interrupted = EndOfTurn;
                                break;
                            }

                        case General.Intent.Logout:
                            {
                                // Log user out of all accounts.
                                await LogUserOut(innerDc);

                                await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale(CalendarMainResponses.LogOut));
                                await innerDc.CancelAllDialogsAsync();
                                if (innerDc.Context.IsSkill())
                                {
                                    interrupted = await innerDc.EndDialogAsync(state.IsAction ? new ActionResult { ActionSuccess = false } : null, cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    interrupted = await innerDc.BeginDialogAsync(InitialDialogId, cancellationToken: cancellationToken);
                                }

                                interrupted = EndOfTurn;
                                break;
                            }
                    }
                }
            }

            return interrupted;
        }

        // Handles introduction/continuation prompt logic.
        private async Task<DialogTurnResult> IntroStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Context.IsSkill())
            {
                // If the bot is in skill mode, skip directly to route and do not prompt
                return await stepContext.NextAsync();
            }
            else
            {
                // If bot is in local mode, prompt with intro or continuation message
                var promptOptions = new PromptOptions
                {
                    Prompt = stepContext.Options as Activity ?? _templateManager.GenerateActivityForLocale(CalendarMainResponses.FirstPromptMessage)
                };

                if (stepContext.Context.Activity.Type == ActivityTypes.ConversationUpdate)
                {
                    promptOptions.Prompt = _templateManager.GenerateActivityForLocale(CalendarMainResponses.CalendarWelcomeMessage);
                }

                return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
            }
        }

        // Handles routing to additional dialogs logic.
        private async Task<DialogTurnResult> RouteStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var a = stepContext.Context.Activity;
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new CalendarSkillState());
            state.IsAction = false;
            var options = new CalendarSkillDialogOptions()
            {
                SubFlowMode = false
            };
            if (a.Type == ActivityTypes.Message && !string.IsNullOrEmpty(a.Text))
            {
                var luisResult = stepContext.Context.TurnState.Get<CalendarLuis>(StateProperties.CalendarLuisResultKey);
                var intent = luisResult?.TopIntent().intent;
                state.InitialIntent = intent.Value;

                var generalResult = stepContext.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                var generalIntent = generalResult?.TopIntent().intent;

                InitializeConfig(state);

                // switch on general intents
                switch (intent)
                {
                    case CalendarLuis.Intent.FindMeetingRoom:
                        {
                            // check whether the meeting room feature supported.
                            if (!string.IsNullOrEmpty(_settings.AzureSearch?.SearchServiceName))
                            {
                                return await stepContext.BeginDialogAsync(_bookMeetingRoomDialog.Id, options);
                            }
                            else
                            {
                                var activity = _templateManager.GenerateActivityForLocale(CalendarMainResponses.FeatureNotAvailable);
                                await stepContext.Context.SendActivityAsync(activity);
                            }

                            break;
                        }

                    case CalendarLuis.Intent.AddCalendarEntryAttribute:
                        {
                            // Determine the exact intent using entities
                            if (luisResult.Entities.MeetingRoom != null || luisResult.Entities.MeetingRoomPatternAny != null || CalendarCommonUtil.ContainMeetingRoomSlot(luisResult))
                            {
                                if (!string.IsNullOrEmpty(_settings.AzureSearch?.SearchServiceName))
                                {
                                    return await stepContext.BeginDialogAsync(_updateMeetingRoomDialog.Id, options);
                                }
                                else
                                {
                                    var activity = _templateManager.GenerateActivityForLocale(CalendarMainResponses.FeatureNotAvailable);
                                    await stepContext.Context.SendActivityAsync(activity);
                                }
                            }
                            else
                            {
                                var activity = _templateManager.GenerateActivityForLocale(CalendarMainResponses.FeatureNotAvailable);
                                await stepContext.Context.SendActivityAsync(activity);
                            }

                            break;
                        }

                    case CalendarLuis.Intent.CreateCalendarEntry:
                        {
                            return await stepContext.BeginDialogAsync(_createEventDialog.Id, options);
                        }

                    case CalendarLuis.Intent.AcceptEventEntry:
                        {
                            return await stepContext.BeginDialogAsync(_changeEventStatusDialog.Id, new ChangeEventStatusDialogOptions(options, EventStatus.Accepted));
                        }

                    case CalendarLuis.Intent.DeleteCalendarEntry:
                        {
                            if (luisResult.Entities.MeetingRoom != null || luisResult.Entities.MeetingRoomPatternAny != null || CalendarCommonUtil.ContainMeetingRoomSlot(luisResult))
                            {
                                if (!string.IsNullOrEmpty(_settings.AzureSearch?.SearchServiceName))
                                {
                                    return await stepContext.BeginDialogAsync(_updateMeetingRoomDialog.Id, options);
                                }
                                else
                                {
                                    var activity = _templateManager.GenerateActivityForLocale(CalendarMainResponses.FeatureNotAvailable);
                                    await stepContext.Context.SendActivityAsync(activity);
                                }
                            }
                            else
                            {
                                return await stepContext.BeginDialogAsync(_changeEventStatusDialog.Id, new ChangeEventStatusDialogOptions(options, EventStatus.Cancelled));
                            }

                            break;
                        }

                    case CalendarLuis.Intent.ChangeCalendarEntry:
                        {
                            if (luisResult.Entities.MeetingRoom != null || luisResult.Entities.MeetingRoomPatternAny != null || CalendarCommonUtil.ContainMeetingRoomSlot(luisResult))
                            {
                                if (!string.IsNullOrEmpty(_settings.AzureSearch?.SearchServiceName))
                                {
                                    return await stepContext.BeginDialogAsync(_updateMeetingRoomDialog.Id, options);
                                }
                                else
                                {
                                    var activity = _templateManager.GenerateActivityForLocale(CalendarMainResponses.FeatureNotAvailable);
                                    await stepContext.Context.SendActivityAsync(activity);
                                }
                            }
                            else
                            {
                                return await stepContext.BeginDialogAsync(_updateEventDialog.Id, options);
                            }

                            break;
                        }

                    case CalendarLuis.Intent.ConnectToMeeting:
                        {
                            return await stepContext.BeginDialogAsync(_joinEventDialog.Id, options);
                        }

                    case CalendarLuis.Intent.FindCalendarEntry:
                    case CalendarLuis.Intent.FindCalendarDetail:
                    case CalendarLuis.Intent.FindCalendarWhen:
                    case CalendarLuis.Intent.FindCalendarWhere:
                    case CalendarLuis.Intent.FindCalendarWho:
                    case CalendarLuis.Intent.FindDuration:
                        {
                            return await stepContext.BeginDialogAsync(_showEventsDialog.Id, new ShowMeetingsDialogOptions(ShowMeetingsDialogOptions.ShowMeetingReason.FirstShowOverview, options));
                        }

                    case CalendarLuis.Intent.TimeRemaining:
                        {
                            return await stepContext.BeginDialogAsync(_timeRemainingDialog.Id);
                        }

                    case CalendarLuis.Intent.CheckAvailability:
                        {
                            if (luisResult.Entities.MeetingRoom != null || luisResult.Entities.MeetingRoomPatternAny != null || CalendarCommonUtil.ContainMeetingRoomSlot(luisResult))
                            {
                                if (!string.IsNullOrEmpty(_settings.AzureSearch?.SearchServiceName))
                                {
                                    state.InitialIntent = CalendarLuis.Intent.FindMeetingRoom;
                                    return await stepContext.BeginDialogAsync(_bookMeetingRoomDialog.Id, options);
                                }
                                else
                                {
                                    var activity = _templateManager.GenerateActivityForLocale(CalendarMainResponses.FeatureNotAvailable);
                                    await stepContext.Context.SendActivityAsync(activity);
                                }
                            }
                            else
                            {
                                return await stepContext.BeginDialogAsync(_checkPersonAvailableDialog.Id);
                            }

                            break;
                        }

                    case CalendarLuis.Intent.ShowNextCalendar:
                    case CalendarLuis.Intent.ShowPreviousCalendar:
                        {
                            return await stepContext.BeginDialogAsync(_showEventsDialog.Id, new ShowMeetingsDialogOptions(ShowMeetingsDialogOptions.ShowMeetingReason.FirstShowOverview, options));
                        }

                    case CalendarLuis.Intent.None:
                        {
                            if (generalIntent == General.Intent.ShowNext || generalIntent == General.Intent.ShowPrevious)
                            {
                                return await stepContext.BeginDialogAsync(_showEventsDialog.Id, new ShowMeetingsDialogOptions(ShowMeetingsDialogOptions.ShowMeetingReason.FirstShowOverview, options));
                            }
                            else
                            {
                                var activity = _templateManager.GenerateActivityForLocale(CalendarSharedResponses.DidntUnderstandMessage);
                                await stepContext.Context.SendActivityAsync(activity);
                            }

                            break;
                        }

                    default:
                        {
                            var activity = _templateManager.GenerateActivityForLocale(CalendarMainResponses.FeatureNotAvailable);
                            await stepContext.Context.SendActivityAsync(activity);
                            break;
                        }
                }
            }
            else if (a.Type == ActivityTypes.Event)
            {
                var ev = a.AsEventActivity();
                if (!string.IsNullOrEmpty(ev.Name))
                {
                    switch (ev.Name)
                    {
                        case Events.DeviceStart:
                            {
                                return await stepContext.BeginDialogAsync(_upcomingEventDialog.Id);
                            }

                        case Events.CreateEvent:
                            {
                                state.IsAction = true;
                                EventInfo actionData = null;
                                if (ev.Value is JObject info)
                                {
                                    actionData = info.ToObject<EventInfo>();
                                    actionData.DigestState(state);
                                }

                                return await stepContext.BeginDialogAsync(_createEventDialog.Id, options);
                            }

                        case Events.UpdateEvent:
                            {
                                state.IsAction = true;
                                UpdateEventInfo actionData = null;
                                if (ev.Value is JObject info)
                                {
                                    actionData = info.ToObject<UpdateEventInfo>();
                                    actionData.DigestState(state);
                                }

                                return await stepContext.BeginDialogAsync(_updateEventDialog.Id, options);
                            }

                        case Events.ShowEvent:
                            {
                                state.IsAction = true;
                                ChooseEventInfo actionData = null;
                                if (ev.Value is JObject info)
                                {
                                    actionData = info.ToObject<ChooseEventInfo>();
                                    actionData.DigestState(state);
                                }

                                return await stepContext.BeginDialogAsync(_showEventsDialog.Id, new ShowMeetingsDialogOptions(ShowMeetingsDialogOptions.ShowMeetingReason.FirstShowOverview, options));
                            }

                        case Events.AcceptEvent:
                            {
                                state.IsAction = true;
                                ChooseEventInfo actionData = null;
                                if (ev.Value is JObject info)
                                {
                                    actionData = info.ToObject<ChooseEventInfo>();
                                    actionData.DigestState(state);
                                }

                                return await stepContext.BeginDialogAsync(_changeEventStatusDialog.Id, new ChangeEventStatusDialogOptions(options, EventStatus.Accepted));
                            }

                        case Events.DeleteEvent:
                            {
                                state.IsAction = true;
                                ChooseEventInfo actionData = null;
                                if (ev.Value is JObject info)
                                {
                                    actionData = info.ToObject<ChooseEventInfo>();
                                    actionData.DigestState(state);
                                }

                                return await stepContext.BeginDialogAsync(_changeEventStatusDialog.Id, new ChangeEventStatusDialogOptions(options, EventStatus.Cancelled));
                            }

                        case Events.JoinEvent:
                            {
                                state.IsAction = true;
                                ChooseEventInfo actionData = null;
                                if (ev.Value is JObject info)
                                {
                                    actionData = info.ToObject<ChooseEventInfo>();
                                    actionData.DigestState(state);
                                }

                                return await stepContext.BeginDialogAsync(_joinEventDialog.Id, options);
                            }

                        case Events.TimeRemaining:
                            {
                                state.IsAction = true;
                                ChooseEventInfo actionData = null;
                                if (ev.Value is JObject info)
                                {
                                    actionData = info.ToObject<ChooseEventInfo>();
                                    actionData.DigestState(state);
                                }

                                return await stepContext.BeginDialogAsync(_timeRemainingDialog.Id);
                            }

                        case Events.Summary:
                            {
                                state.IsAction = true;
                                DateInfo actionData = null;
                                if (ev.Value is JObject info)
                                {
                                    actionData = info.ToObject<DateInfo>();
                                    actionData.DigestState(state);
                                }

                                return await stepContext.BeginDialogAsync(_showEventsDialog.Id, new ShowMeetingsDialogOptions(ShowMeetingsDialogOptions.ShowMeetingReason.Summary, options));
                            }

                        default:
                            {
                                await stepContext.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown Event '{ev.Name ?? "undefined"}' was received but not processed."));
                                break;
                            }
                    }
                }
            }

            // If activity was unhandled, flow should continue to next step
            return await stepContext.NextAsync();
        }

        // Handles conversation cleanup.
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new CalendarSkillState());
            if (stepContext.Context.IsSkill())
            {
                var result = stepContext.Result;

                if (state.IsAction && result as ActionResult == null)
                {
                    result = new ActionResult() { ActionSuccess = false };
                }

                state.Clear();
                return await stepContext.EndDialogAsync(result, cancellationToken);
            }
            else
            {
                state.Clear();
                return await stepContext.ReplaceDialogAsync(InitialDialogId, _templateManager.GenerateActivityForLocale(CalendarMainResponses.CompletedMessage), cancellationToken);
            }
        }

        private async Task LogUserOut(DialogContext dc)
        {
            IUserTokenProvider tokenProvider;
            var supported = dc.Context.Adapter is IUserTokenProvider;
            if (supported)
            {
                tokenProvider = (IUserTokenProvider)dc.Context.Adapter;

                // Sign out user
                var tokens = await tokenProvider.GetTokenStatusAsync(dc.Context, dc.Context.Activity.From.Id);
                foreach (var token in tokens)
                {
                    await tokenProvider.SignOutUserAsync(dc.Context, token.ConnectionName);
                }

                // Cancel all active dialogs
                await dc.CancelAllDialogsAsync();
            }
            else
            {
                throw new InvalidOperationException("OAuthPrompt.SignOutUser(): not supported by the current adapter");
            }
        }

        private void InitializeConfig(CalendarSkillState state)
        {
            // Initialize PageSize when the first input comes.
            if (state.PageSize <= 0)
            {
                var pageSize = _settings.DisplaySize;
                state.PageSize = pageSize <= 0 || pageSize > CalendarCommonUtil.MaxDisplaySize ? CalendarCommonUtil.MaxDisplaySize : pageSize;
            }
        }

        private class Events
        {
            public const string DeviceStart = "DeviceStart";
            public const string CreateEvent = "CreateEvent";
            public const string UpdateEvent = "UpdateEvent";
            public const string ShowEvent = "ShowEvent";
            public const string AcceptEvent = "AcceptEvent";
            public const string DeleteEvent = "DeleteEvent";
            public const string JoinEvent = "JoinEvent";
            public const string TimeRemaining = "TimeRemaining";
            public const string Summary = "Summary";
        }
    }
}
