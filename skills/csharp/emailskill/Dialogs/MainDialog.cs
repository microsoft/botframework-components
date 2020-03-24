﻿using System;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Models;
using EmailSkill.Models.Action;
using EmailSkill.Responses.Main;
using EmailSkill.Responses.Shared;
using EmailSkill.Services;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;
using SkillServiceLibrary.Utilities;

namespace EmailSkill.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        private BotSettings _settings;
        private BotServices _services;
        private LocaleTemplateManager _templateManager;
        private IStatePropertyAccessor<EmailSkillState> _stateAccessor;
        private Dialog _forwardEmailDialog;
        private Dialog _sendEmailDialog;
        private Dialog _showEmailDialog;
        private Dialog _replyEmailDialog;
        private Dialog _deleteEmailDialog;

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
            _stateAccessor = conversationState.CreateProperty<EmailSkillState>(nameof(EmailSkillState));

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
            _forwardEmailDialog = serviceProvider.GetService<ForwardEmailDialog>() ?? throw new ArgumentNullException(nameof(_forwardEmailDialog));
            _sendEmailDialog = serviceProvider.GetService<SendEmailDialog>() ?? throw new ArgumentNullException(nameof(_sendEmailDialog));
            _showEmailDialog = serviceProvider.GetService<ShowEmailDialog>() ?? throw new ArgumentNullException(nameof(_showEmailDialog));
            _replyEmailDialog = serviceProvider.GetService<ReplyEmailDialog>() ?? throw new ArgumentNullException(nameof(_replyEmailDialog));
            _deleteEmailDialog = serviceProvider.GetService<DeleteEmailDialog>() ?? throw new ArgumentNullException(nameof(_deleteEmailDialog));
            AddDialog(_forwardEmailDialog);
            AddDialog(_sendEmailDialog);
            AddDialog(_showEmailDialog);
            AddDialog(_replyEmailDialog);
            AddDialog(_deleteEmailDialog);

            GetReadingDisplayConfig();
        }

        // Runs when the dialog is started.
        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext innerDc, object options, CancellationToken cancellationToken = default)
        {
            if (innerDc.Context.Activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(innerDc.Context.Activity.Text))
            {
                // Get cognitive models for the current locale.
                var localizedServices = _services.GetCognitiveModels();

                // Run LUIS recognition on Skill model and store result in turn state.
                localizedServices.LuisServices.TryGetValue("Email", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<EmailLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.EmailLuisResult, skillResult);
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
                localizedServices.LuisServices.TryGetValue("Email", out var skillLuisService);
                if (skillLuisService != null)
                {
                    var skillResult = await skillLuisService.RecognizeAsync<EmailLuis>(innerDc.Context, cancellationToken);
                    innerDc.Context.TurnState.Add(StateProperties.EmailLuisResult, skillResult);
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
                                await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale(EmailSharedResponses.CancellingMessage));
                                await innerDc.CancelAllDialogsAsync();
                                if (innerDc.Context.IsSkill())
                                {
                                    var state = await _stateAccessor.GetAsync(innerDc.Context, () => new EmailSkillState());
                                    interrupted = await innerDc.EndDialogAsync(state.IsAction ? new ActionResult(false) : null, cancellationToken: cancellationToken);
                                }
                                else
                                {
                                    interrupted = await innerDc.BeginDialogAsync(InitialDialogId, cancellationToken: cancellationToken);
                                }

                                break;
                            }

                        case General.Intent.Help:
                            {
                                await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale(EmailMainResponses.HelpMessage));
                                await innerDc.RepromptDialogAsync();
                                interrupted = EndOfTurn;
                                break;
                            }

                        case General.Intent.Logout:
                            {
                                // Log user out of all accounts.
                                await LogUserOut(innerDc);

                                await innerDc.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale(EmailMainResponses.LogOut));
                                await innerDc.CancelAllDialogsAsync();
                                if (innerDc.Context.IsSkill())
                                {
                                    var state = await _stateAccessor.GetAsync(innerDc.Context, () => new EmailSkillState());
                                    interrupted = await innerDc.EndDialogAsync(state.IsAction ? new ActionResult(false) : null, cancellationToken: cancellationToken);
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
                return await stepContext.NextAsync();
            }
            else
            {
                // If bot is in local mode, prompt with intro or continuation message
                var promptOptions = new PromptOptions
                {
                    Prompt = stepContext.Options as Activity ?? _templateManager.GenerateActivityForLocale(EmailMainResponses.FirstPromptMessage)
                };

                if (stepContext.Context.Activity.Type == ActivityTypes.ConversationUpdate)
                {
                    promptOptions.Prompt = _templateManager.GenerateActivityForLocale(EmailMainResponses.EmailWelcomeMessage);
                }

                return await stepContext.PromptAsync(nameof(TextPrompt), promptOptions, cancellationToken);
            }
        }

        // Handles routing to additional dialogs logic.
        private async Task<DialogTurnResult> RouteStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var activity = stepContext.Context.Activity;
            var state = await _stateAccessor.GetAsync(stepContext.Context, () => new EmailSkillState());
            state.IsAction = false;

            if (activity.Type == ActivityTypes.Message && !string.IsNullOrEmpty(activity.Text))
            {
                var result = stepContext.Context.TurnState.Get<EmailLuis>(StateProperties.EmailLuisResult);
                var intent = result?.TopIntent().intent;

                var generalResult = stepContext.Context.TurnState.Get<General>(StateProperties.GeneralLuisResult);
                var generalIntent = generalResult?.TopIntent().intent;

                var skillOptions = new EmailSkillDialogOptions
                {
                    SubFlowMode = false
                };

                switch (intent)
                {
                    case EmailLuis.Intent.SendEmail:
                        {
                            return await stepContext.BeginDialogAsync(nameof(SendEmailDialog), skillOptions);
                        }

                    case EmailLuis.Intent.Forward:
                        {
                            return await stepContext.BeginDialogAsync(nameof(ForwardEmailDialog), skillOptions);
                        }

                    case EmailLuis.Intent.Reply:
                        {
                            return await stepContext.BeginDialogAsync(nameof(ReplyEmailDialog), skillOptions);
                        }

                    case EmailLuis.Intent.SearchMessages:
                    case EmailLuis.Intent.CheckMessages:
                    case EmailLuis.Intent.ReadAloud:
                    case EmailLuis.Intent.QueryLastText:
                        {
                            return await stepContext.BeginDialogAsync(nameof(ShowEmailDialog), skillOptions);
                        }

                    case EmailLuis.Intent.Delete:
                        {
                            return await stepContext.BeginDialogAsync(nameof(DeleteEmailDialog), skillOptions);
                        }

                    case EmailLuis.Intent.ShowNext:
                    case EmailLuis.Intent.ShowPrevious:
                    case EmailLuis.Intent.None:
                        {
                            if (intent == EmailLuis.Intent.ShowNext
                                || intent == EmailLuis.Intent.ShowPrevious
                                || generalIntent == General.Intent.ShowNext
                                || generalIntent == General.Intent.ShowPrevious)
                            {
                                return await stepContext.BeginDialogAsync(nameof(ShowEmailDialog), skillOptions);
                            }
                            else
                            {
                                await stepContext.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale(EmailSharedResponses.DidntUnderstandMessage));
                            }

                            break;
                        }

                    default:
                        {
                            await stepContext.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale(EmailMainResponses.FeatureNotAvailable));
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
                        case "SendEmail":
                            {
                                EmailInfo actionData = null;

                                var eventValue = activity.Value as JObject;
                                if (eventValue != null)
                                {
                                    actionData = eventValue.ToObject<EmailInfo>();
                                    await DigestActionInput(stepContext, actionData);
                                }

                                state.IsAction = true;
                                return await stepContext.BeginDialogAsync(nameof(SendEmailDialog), new EmailSkillDialogOptions());
                            }

                        case "DeleteEmail":
                            {
                                state.IsAction = true;
                                return await stepContext.BeginDialogAsync(nameof(DeleteEmailDialog), new EmailSkillDialogOptions());
                            }

                        case "ReplyEmail":
                            {
                                ReplyEmailInfo actionData = null;

                                var eventValue = activity.Value as JObject;
                                if (eventValue != null)
                                {
                                    actionData = eventValue.ToObject<ReplyEmailInfo>();
                                    await DigestActionInput(stepContext, actionData);
                                }

                                state.IsAction = true;
                                return await stepContext.BeginDialogAsync(nameof(ReplyEmailDialog), new EmailSkillDialogOptions());
                            }

                        case "ForwardEmail":
                            {
                                ForwardEmailInfo actionData = null;

                                var eventValue = activity.Value as JObject;
                                if (eventValue != null)
                                {
                                    actionData = eventValue.ToObject<ForwardEmailInfo>();
                                    await DigestActionInput(stepContext, actionData);
                                }

                                state.IsAction = true;
                                return await stepContext.BeginDialogAsync(nameof(ForwardEmailDialog), new EmailSkillDialogOptions());
                            }

                        case "EmailSummary":
                            {
                                state.IsAction = true;
                                return await stepContext.BeginDialogAsync(nameof(ShowEmailDialog), new EmailSkillDialogOptions());
                            }

                        default:

                            // todo: move the response to lg
                            await stepContext.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Unknown Event '{eventActivity.Name ?? "undefined"}' was received but not processed."));

                            break;
                    }
                }
            }

            // If activity was unhandled, flow should continue to next step
            return await stepContext.NextAsync();
        }

        // Handles conversation cleanup.
        private async Task<DialogTurnResult> FinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if (stepContext.Context.IsSkill())
            {
                return await stepContext.EndDialogAsync(stepContext.Result, cancellationToken);
            }
            else
            {
                return await stepContext.ReplaceDialogAsync(InitialDialogId, _templateManager.GenerateActivityForLocale(EmailMainResponses.CompletedMessage), cancellationToken);
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

        private void GetReadingDisplayConfig()
        {
            if (_settings.DisplaySize > 0)
            {
                ConfigData.GetInstance().MaxDisplaySize = _settings.DisplaySize;
            }
        }

        private async Task DigestActionInput(DialogContext dc, EmailInfo emailInfo)
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new EmailSkillState());
            state.Subject = emailInfo.Subject;
            state.Content = emailInfo.Content;
            if (emailInfo.Reciever != null)
            {
                var recieverList = emailInfo.Reciever;
                foreach (var emailAddress in recieverList)
                {
                    if (!state.FindContactInfor.ContactsNameList.Contains(emailAddress))
                    {
                        state.FindContactInfor.ContactsNameList.Add(emailAddress);
                    }
                }
            }
        }

        private async Task DigestActionInput(DialogContext dc, ReplyEmailInfo replyEmailInfo)
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new EmailSkillState());
            state.Content = replyEmailInfo.ReplyMessage;
        }

        private async Task DigestActionInput(DialogContext dc, ForwardEmailInfo forwardEmailInfo)
        {
            var state = await _stateAccessor.GetAsync(dc.Context, () => new EmailSkillState());
            state.Content = forwardEmailInfo.ForwardMessage;
            if (forwardEmailInfo.ForwardReciever != null)
            {
                var recieverList = forwardEmailInfo.ForwardReciever;
                foreach (var emailAddress in recieverList)
                {
                    if (!state.FindContactInfor.ContactsNameList.Contains(emailAddress))
                    {
                        state.FindContactInfor.ContactsNameList.Add(emailAddress);
                    }
                }
            }
        }
    }
}
