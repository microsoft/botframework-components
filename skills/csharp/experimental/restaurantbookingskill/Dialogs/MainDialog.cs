// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using RestaurantBookingSkill.Models.Action;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Dialogs;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Bot.Solutions.Skills.Models;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using RestaurantBookingSkill.Models;
using RestaurantBookingSkill.Models.Action;
using RestaurantBookingSkill.Responses.Main;
using RestaurantBookingSkill.Responses.Shared;
using RestaurantBookingSkill.Services;
using RestaurantBookingSkill.Utilities;
using SkillServiceLibrary.Utilities;

namespace RestaurantBookingSkill.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly BotSettings _settings;
        private readonly BotServices _services;
        private readonly LocaleTemplateManager _localeTemplateManager;
        private readonly UserState _userState;
        private readonly ConversationState _conversationState;
        private readonly IStatePropertyAccessor<RestaurantBookingState> _conversationStateAccessor;

        public MainDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient)
            : base(nameof(MainDialog))
        {
            _settings = serviceProvider.GetService<BotSettings>();
            _services = serviceProvider.GetService<BotServices>();
            _localeTemplateManager = serviceProvider.GetService<LocaleTemplateManager>();
            TelemetryClient = telemetryClient;

            // Create conversation state properties
            var conversationState = serviceProvider.GetService<ConversationState>();
            _conversationStateAccessor = conversationState.CreateProperty<RestaurantBookingState>(nameof(BookingDialog));

            var steps = new WaterfallStep[]
            {
                IntroStepAsync,
                RouteStepAsync,
                FinalStepAsync,
            };

            AddDialog(new WaterfallDialog(nameof(MainDialog), steps));
            AddDialog(new TextPrompt(nameof(TextPrompt)));
            InitialDialogId = nameof(MainDialog);

            // RegisterDialogs
            AddDialog(serviceProvider.GetService<BookingDialog>() ?? throw new ArgumentNullException(nameof(BookingDialog)));
        }

        // Runs when the dialog is started.
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(innerDc.Context.Activity.Text))
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("Restaurant", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<ReservationLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.ReservationLuis, skillResult);
                }
                else
                {
                    throw new Exception("The skill LUIS Model could not be found in your Bot Services configuration.");
                }

                // Run LUIS recognition on General model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("General", out var generalLuisService);
                if (generalLuisService != null)
                {
                    var generalResult = await generalLuisService.RecognizeAsync<General>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.GeneralLuisResult, generalResult);
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

                // Run LUIS recognition on Skill model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("Restaurant", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<ReservationLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.ReservationLuis, skillResult);
                }
                else
                {
                    throw new Exception("The skill LUIS Model could not be found in your Bot Services configuration.");
                }

                // Run LUIS recognition on General model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("General", out var generalLuisService);
                if (generalLuisService != null)
                {
                    var generalResult = await generalLuisService.RecognizeAsync<General>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.GeneralLuisResult, generalResult);
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
                var generalResult = innerDc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResult);
                (var generalIntent, var generalScore) = generalResult.TopIntent();

                if (generalScore > 0.5)
                {
                    switch (generalIntent)
                    {
                        case General.Intent.Cancel:
                            {
                                var state = await _conversationStateAccessor.GetAsync(innerDc.Context, () => new RestaurantBookingState(), cancellationToken);
                                state.Clear();

                                await innerDc.Context.SendActivityAsync(_localeTemplateManager.GenerateActivity(RestaurantBookingSharedResponses.CancellingMessage), cancellationToken);
                                await innerDc.CancelAllDialogsAsync(cancellationToken);
                                if (innerDc.Context.IsSkill())
                                {
                                    interrupted = await innerDc.EndDialogAsync(state.IsAction ? new ActionResult { ActionSuccess = false } : null, cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    interrupted = await innerDc.BeginDialogAsync(InitialDialogId, cancellationToken: cancellationToken);
                                }

                                break;
                            }

                        case General.Intent.Help:
                            {
                                await innerDc.Context.SendActivityAsync(_localeTemplateManager.GenerateActivity(RestaurantBookingMainResponses.HelpMessage), cancellationToken);
                                await innerDc.RepromptDialogAsync(cancellationToken);
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
                return await stepContext.NextAsync(cancellationToken: cancellationToken);
            }

            // If bot is in local mode, prompt with intro or continuation message
            var promptOptions = new PromptOptions
            {
                Prompt = stepContext.Options as Activity ?? _localeTemplateManager.GenerateActivity(RestaurantBookingMainResponses.FirstPromptMessage)
            };

            if (stepContext.Context.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                promptOptions.Prompt = _localeTemplateManager.GenerateActivity(RestaurantBookingMainResponses.WelcomeMessage);
            }

            return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
        }

        // Handles routing to additional dialogs logic.
        private async Task<DialogTurnResult> RouteStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _conversationStateAccessor.GetAsync(stepContext.Context, () => new RestaurantBookingState(), cancellationToken);
            state.IsAction = false;

            var activity = stepContext.Context.Activity;
            if (activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(activity.Text))
            {
                var result = stepContext.Context.TurnState.Get<ReservationLuis>(StateProperties.ReservationLuis);
                var intent = result?.TopIntent().intent;

                switch (intent)
                {
                    case ReservationLuis.Intent.Reservation:
                        {
                            return await stepContext.BeginDialogAsync(nameof(BookingDialog), cancellationToken: cancellationToken);
                        }

                    case ReservationLuis.Intent.None:
                        {
                            // No intent was identified, send confused message
                            await stepContext.Context.SendActivityAsync(_localeTemplateManager.GenerateActivity(RestaurantBookingSharedResponses.DidntUnderstandMessage), cancellationToken);
                            break;
                        }

                    default:
                        {
                            // intent was identified but not yet implemented
                            await stepContext.Context.SendActivityAsync(_localeTemplateManager.GenerateActivity(RestaurantBookingSharedResponses.DidntUnderstandMessage), cancellationToken);
                            break;
                        }
                }
            }
            else if (activity.Type == ActivityTypes.Event)
            {
                // Handle skill actions here
                var eventActivity = activity.AsEventActivity();

                if (!string.IsNullOrEmpty(eventActivity.Name))
                {
                    switch (eventActivity.Name)
                    {
                        // Each Action in the Manifest will have an associated Name which will be on incoming Event activities
                        case "RestaurantReservation":
                            {
                                state.IsAction = true;
                                ReservationInfo actionData = null;
                                var eventValue = activity.Value as JObject;
                                if (eventValue != null)
                                {
                                    actionData = eventValue.ToObject<ReservationInfo>();
                                    await DigestActionInput(stepContext, actionData, cancellationToken);
                                }

                                return await stepContext.BeginDialogAsync(nameof(BookingDialog), cancellationToken: cancellationToken);
                            }

                        default:
                            {
                                // todo: move the response to lg
                                await stepContext.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown Event '{eventActivity.Name ?? "undefined"}' was received but not processed."), cancellationToken);

                                break;
                            }
                    }
                }
            }

            // If activity was unhandled, flow should continue to next step
            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }

        // Handles conversation cleanup.
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _conversationStateAccessor.GetAsync(stepContext.Context, () => new RestaurantBookingState(), cancellationToken);

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
                return await stepContext.ReplaceDialogAsync(InitialDialogId, _localeTemplateManager.GenerateActivity(RestaurantBookingMainResponses.CompletedMessage), cancellationToken);
            }
        }

        private async Task DigestActionInput(DialogContext dc, ReservationInfo reservationInfo, CancellationToken cancellationToken)
        {
            var state = await _conversationStateAccessor.GetAsync(dc.Context, () => new RestaurantBookingState(), cancellationToken);
            state.Booking.Category = reservationInfo.FoodType;
            state.Booking.ReservationDate = DateTime.Parse(reservationInfo.Date);
            state.Booking.ReservationTime = DateTime.Parse(reservationInfo.Time);
            state.Booking.AttendeeCount = reservationInfo.AttendeeCount;

            // Note: Now there is no actual action to book a place. So we only save Name.
            state.Booking.BookingPlace = new BookingPlace() { Name = reservationInfo.RestaurantName };
        }
    }
}