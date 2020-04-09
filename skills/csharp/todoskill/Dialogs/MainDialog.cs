// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using SkillServiceLibrary.Utilities;
using ToDoSkill.Models;
using ToDoSkill.Models.Action;
using ToDoSkill.Responses.Main;
using ToDoSkill.Services;
using ToDoSkill.Utilities;

namespace ToDoSkill.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private readonly BotSettings _settings;
        private readonly BotServices _services;
        private readonly LocaleTemplateManager _templateManager;
        private readonly IStatePropertyAccessor<ToDoSkillState> _stateAccessor;
        private readonly Dialog _addToDoItemDialog;
        private readonly Dialog _markToDoItemDialog;
        private readonly Dialog _deleteToDoItemDialog;
        private readonly Dialog _showToDoItemDialog;

        public MainDialog(
            IServiceProvider serviceProvider)
            : base(nameof(MainDialog))
        {
            _settings = serviceProvider.GetService<BotSettings>();
            _services = serviceProvider.GetService<BotServices>();
            _templateManager = serviceProvider.GetService<LocaleTemplateManager>();

            // Create conversation state properties
            var conversationState = serviceProvider.GetService<ConversationState>();
            _stateAccessor = conversationState.CreateProperty<ToDoSkillState>(nameof(ToDoSkillState));

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
            _addToDoItemDialog = serviceProvider.GetService<AddToDoItemDialog>() ?? throw new ArgumentNullException(nameof(AddToDoItemDialog));
            _markToDoItemDialog = serviceProvider.GetService<MarkToDoItemDialog>() ?? throw new ArgumentNullException(nameof(MarkToDoItemDialog));
            _deleteToDoItemDialog = serviceProvider.GetService<DeleteToDoItemDialog>() ?? throw new ArgumentNullException(nameof(DeleteToDoItemDialog));
            _showToDoItemDialog = serviceProvider.GetService<ShowToDoItemDialog>() ?? throw new ArgumentNullException(nameof(ShowToDoItemDialog));
            AddDialog(_addToDoItemDialog);
            AddDialog(_markToDoItemDialog);
            AddDialog(_deleteToDoItemDialog);
            AddDialog(_showToDoItemDialog);
        }

        // Runs when the dialog is started.
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(innerDc.Context.Activity.Text))
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("ToDo", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<ToDoLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.ToDoLuisResultKey, skillResult);
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
                    innerDc.Context.TurnState.Add(StateProperties.GeneralLuisResultKey, generalResult);
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
                localizedServices.LuisServices.TryGetValue("ToDo", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<ToDoLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.ToDoLuisResultKey, skillResult);
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
                    innerDc.Context.TurnState.Add(StateProperties.GeneralLuisResultKey, generalResult);
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
                var generalResult = innerDc.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                (var generalIntent, var generalScore) = generalResult.TopIntent();

                if (generalScore > 0.5)
                {
                    switch (generalIntent)
                    {
                        case General.Intent.Cancel:
                            {
                                await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale(ToDoMainResponses.CancelMessage), cancellationToken);
                                await innerDc.CancelAllDialogsAsync();
                                if (innerDc.Context.IsSkill())
                                {
                                    var state = await _stateAccessor.GetAsync(innerDc.Context, () => new ToDoSkillState(), cancellationToken);
                                    interrupted = await innerDc.EndDialogAsync(state.IsAction ? new TodoListInfo { ActionSuccess = false } : null, cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    interrupted = await innerDc.BeginDialogAsync(InitialDialogId, cancellationToken: cancellationToken);
                                }

                                break;
                            }

                        case General.Intent.Help:
                            {
                                await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale(ToDoMainResponses.HelpMessage), cancellationToken);
                                await innerDc.RepromptDialogAsync(cancellationToken);
                                interrupted = EndOfTurn;
                                break;
                            }

                        case General.Intent.Logout:
                            {
                                // Log user out of all accounts.
                                await LogUserOutAsync(innerDc, cancellationToken);

                                await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale(ToDoMainResponses.LogOut), cancellationToken);
                                await innerDc.CancelAllDialogsAsync(cancellationToken);
                                if (innerDc.Context.IsSkill())
                                {
                                    interrupted = await innerDc.EndDialogAsync(cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    interrupted = await innerDc.BeginDialogAsync(InitialDialogId, cancellationToken: cancellationToken);
                                }

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
                Prompt = stepContext.Options as Activity ?? _templateManager.GenerateActivityForLocale(ToDoMainResponses.FirstPromptMessage)
            };

            if (stepContext.Context.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                promptOptions.Prompt = _templateManager.GenerateActivityForLocale(ToDoMainResponses.ToDoWelcomeMessage);
            }

            return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
        }

        // Handles routing to additional dialogs logic.
        private async Task<DialogTurnResult> RouteStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var activity = stepContext.Context.Activity;

            if (activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(activity.Text))
            {
                // Initialize the PageSize and ReadSize parameters in state from configuration
                var state = await _stateAccessor.GetAsync(stepContext.Context, () => new ToDoSkillState(), cancellationToken);
                InitializeConfig(state);

                var luisResult = stepContext.Context.TurnState.Get<ToDoLuis>(StateProperties.ToDoLuisResultKey);
                var intent = luisResult?.TopIntent().intent;
                var generalLuisResult = stepContext.Context.TurnState.Get<General>(StateProperties.GeneralLuisResultKey);
                var generalTopIntent = generalLuisResult?.TopIntent().intent;

                switch (intent)
                {
                    case ToDoLuis.Intent.AddToDo:
                        {
                            return await stepContext.BeginDialogAsync(nameof(AddToDoItemDialog), cancellationToken: cancellationToken);
                        }

                    case ToDoLuis.Intent.MarkToDo:
                        {
                            return await stepContext.BeginDialogAsync(nameof(MarkToDoItemDialog), cancellationToken: cancellationToken);
                        }

                    case ToDoLuis.Intent.DeleteToDo:
                        {
                            return await stepContext.BeginDialogAsync(nameof(DeleteToDoItemDialog), cancellationToken: cancellationToken);
                        }

                    case ToDoLuis.Intent.ShowNextPage:
                    case ToDoLuis.Intent.ShowPreviousPage:
                    case ToDoLuis.Intent.ShowToDo:
                        {
                            return await stepContext.BeginDialogAsync(nameof(ShowToDoItemDialog), cancellationToken: cancellationToken);
                        }

                    case ToDoLuis.Intent.None:
                        {
                            if (generalTopIntent == General.Intent.ShowNext
                                || generalTopIntent == General.Intent.ShowPrevious)
                            {
                                return await stepContext.BeginDialogAsync(nameof(ShowToDoItemDialog), cancellationToken: cancellationToken);
                            }
                            else
                            {
                                // No intent was identified, send confused message
                                var response = _templateManager.GenerateActivityForLocale(ToDoMainResponses.DidntUnderstandMessage);
                                await stepContext.Context.SendActivityAsync(response, cancellationToken);
                            }

                            break;
                        }

                    default:
                        {
                            // intent was identified but not yet implemented
                            var response = _templateManager.GenerateActivityForLocale(ToDoMainResponses.FeatureNotAvailable);
                            await stepContext.Context.SendActivityAsync(response, cancellationToken);
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
                    var state = await _stateAccessor.GetAsync(stepContext.Context, () => new ToDoSkillState(), cancellationToken);
                    InitializeConfig(state);

                    switch (eventActivity.Name)
                    {
                        // Each Action in the Manifest will have an associated Name which will be on incoming Event activities
                        case ActionNames.AddToDo:
                            {
                                await DigestActionInput(stepContext, activity.Value, cancellationToken);
                                state.IsAction = true;
                                state.AddDupTask = true;
                                return await stepContext.BeginDialogAsync(nameof(AddToDoItemDialog), cancellationToken: cancellationToken);
                            }

                        case ActionNames.DeleteToDo:
                            {
                                await DigestActionInput(stepContext, activity.Value, cancellationToken);
                                state.IsAction = true;
                                return await stepContext.BeginDialogAsync(nameof(DeleteToDoItemDialog), cancellationToken: cancellationToken);
                            }

                        case ActionNames.DeleteAll:
                            {
                                await DigestActionInput(stepContext, activity.Value, cancellationToken);
                                state.MarkOrDeleteAllTasksFlag = true;
                                state.DeleteTaskConfirmation = true;
                                state.IsAction = true;
                                return await stepContext.BeginDialogAsync(nameof(DeleteToDoItemDialog), cancellationToken: cancellationToken);
                            }

                        case ActionNames.MarkToDo:
                            {
                                await DigestActionInput(stepContext, activity.Value, cancellationToken);
                                state.IsAction = true;
                                return await stepContext.BeginDialogAsync(nameof(MarkToDoItemDialog), cancellationToken: cancellationToken);
                            }

                        case ActionNames.MarkAll:
                            {
                                await DigestActionInput(stepContext, activity.Value, cancellationToken);
                                state.MarkOrDeleteAllTasksFlag = true;
                                state.IsAction = true;
                                return await stepContext.BeginDialogAsync(nameof(MarkToDoItemDialog), cancellationToken: cancellationToken);
                            }

                        case ActionNames.ShowToDo:
                            {
                                await DigestActionInput(stepContext, activity.Value, cancellationToken);
                                state.IsAction = true;
                                return await stepContext.BeginDialogAsync(nameof(ShowToDoItemDialog), cancellationToken: cancellationToken);
                            }

                        default:

                            // todo: move the response to lg
                            await stepContext.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown Event '{eventActivity.Name ?? "undefined"}' was received but not processed."), cancellationToken);

                            break;
                    }
                }
            }

            // If activity was unhandled, flow should continue to next step
            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }

        // Handles conversation cleanup.
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new ToDoSkillState(), cancellationToken);

            if (stepContext.Context.IsSkill())
            {
                var result = stepContext.Result;

                if (state.IsAction && result == null)
                {
                    result = new TodoListInfo() { ActionSuccess = false };
                }

                state.Clear();
                return await stepContext.EndDialogAsync(result, cancellationToken);
            }
            else
            {
                state.Clear();
                return await stepContext.ReplaceDialogAsync(InitialDialogId, _templateManager.GenerateActivityForLocale(ToDoMainResponses.CompletedMessage), cancellationToken);
            }
        }

        private async Task LogUserOutAsync(DialogContext dc, CancellationToken cancellationToken)
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
                    await tokenProvider.SignOutUserAsync(dc.Context, token.ConnectionName, cancellationToken: cancellationToken);
                }

                // Cancel all active dialogs
                await dc.CancelAllDialogsAsync(cancellationToken);
            }
            else
            {
                throw new InvalidOperationException("OAuthPrompt.SignOutUser(): not supported by the current adapter");
            }
        }

        private void InitializeConfig(ToDoSkillState state)
        {
            // Initialize PageSize and TaskServiceType when the first input comes.
            if (state.PageSize <= 0)
            {
                var pageSize = _settings.DisplaySize;
                state.PageSize = pageSize <= 0 ? ToDoCommonUtil.DefaultDisplaySize : pageSize;
            }

            if (state.TaskServiceType == ServiceProviderType.Other)
            {
                state.TaskServiceType = ServiceProviderType.Outlook;
                if (!string.IsNullOrEmpty(_settings.TaskServiceProvider))
                {
                    if (_settings.TaskServiceProvider.Equals(ServiceProviderType.OneNote.ToString(), StringComparison.InvariantCultureIgnoreCase))
                    {
                        state.TaskServiceType = ServiceProviderType.OneNote;
                    }
                }
            }
        }

        private async Task DigestActionInput(DialogContext dc, object actionInput, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new ToDoSkillState(), cancellationToken);
            var value = actionInput as JObject;

            try
            {
                var actionData = value.ToObject<ToDoInfo>();
                state.ListType = actionData.ListType;
                state.TaskContent = actionData.TaskName;
                state.TaskContentML = actionData.TaskName;
                return;
            }
            catch
            {
            }
        }
    }
}
