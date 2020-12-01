// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using ITSMSkill.Dialogs.Teams;
using ITSMSkill.Extensions;
using ITSMSkill.Extensions.Teams.TaskModule;
using ITSMSkill.Models.ServiceNow;
using ITSMSkill.Models.UpdateActivity;
using ITSMSkill.Proactive;
using ITSMSkill.Proactive.Subscription;
using ITSMSkill.Responses.Main;
using ITSMSkill.Services;
using ITSMSkill.TeamsChannels.Invoke;
using ITSMSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.ApplicationInsights;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Proactive;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ITSMSkill.Bots
{
    public class DefaultActivityHandler<T> : TeamsActivityHandler
        where T : Dialog
    {
        private readonly Dialog _dialog;
        private readonly BotState _conversationState;
        private readonly BotState _userState;
        private readonly ProactiveState _proactiveState;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly IStatePropertyAccessor<ProactiveModel> _proactiveStateAccessor;
        private readonly IStatePropertyAccessor<ActivityReferenceMap> _activityReferenceMapAccessor;
        private readonly LocaleTemplateManager _templateManager;
        private readonly BotSettings _botSettings;
        private readonly BotServices _botServices;
        private readonly IServiceManager _serviceManager;
        private readonly BotTelemetryClient _botTelemetryClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITeamsActivity<AdaptiveCard> _teamsTicketUpdateActivity;
        private readonly IStatePropertyAccessor<ConversationReferenceMap> _proactiveStateConversationReferenceMapAccessor;
        private readonly SubscriptionManager _subscriptionManager;

        public DefaultActivityHandler(IServiceProvider serviceProvider, T dialog)
        {
            _dialog = dialog;
            _dialog.TelemetryClient = serviceProvider.GetService<IBotTelemetryClient>();
            _conversationState = serviceProvider.GetService<ConversationState>();
            _userState = serviceProvider.GetService<UserState>();
            _proactiveState = serviceProvider.GetService<ProactiveState>();
            _dialogStateAccessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));
            _proactiveStateAccessor = _proactiveState.CreateProperty<ProactiveModel>(nameof(ProactiveModel));
            _activityReferenceMapAccessor = _proactiveState.CreateProperty<ActivityReferenceMap>(nameof(ActivityReferenceMap));
            _proactiveStateConversationReferenceMapAccessor = _proactiveState.CreateProperty<ConversationReferenceMap>(nameof(ConversationReferenceMap));
            _botSettings = serviceProvider.GetService<BotSettings>();
            _templateManager = serviceProvider.GetService<LocaleTemplateManager>();
            _serviceProvider = serviceProvider;
            _teamsTicketUpdateActivity = serviceProvider.GetService<ITeamsActivity<AdaptiveCard>>();
            _subscriptionManager = serviceProvider.GetService<SubscriptionManager>();
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            await base.OnTurnAsync(turnContext, cancellationToken);

            // Save any state changes that might have occured during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
            await _userState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            await turnContext.SendActivityAsync(_templateManager.GenerateActivity(MainResponses.WelcomeMessage), cancellationToken);
            await _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
        }

        protected override Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // directline speech occasionally sends empty message activities that should be ignored
            var activity = turnContext.Activity;
            if (activity.ChannelId == Channels.DirectlineSpeech && activity.Type == ActivityTypes.Message && string.IsNullOrEmpty(activity.Text))
            {
                return Task.CompletedTask;
            }

            return _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
        }

        protected override async Task OnEventActivityAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            var ev = turnContext.Activity.AsEventActivity();
            var value = ev.Value?.ToString();

            ActivityReferenceMap activityReferenceMap = await _activityReferenceMapAccessor.GetAsync(
            turnContext,
            () => new ActivityReferenceMap(),
            cancellationToken)
            .ConfigureAwait(false);

            switch (ev.Name)
            {
                case ServiceNowEvents.Proactive:
                    {
                        var eventData = JsonConvert.DeserializeObject<ServiceNowNotification>(turnContext.Activity.Value.ToString());

                        var proactiveSubscriptions = await _subscriptionManager.GetSubscriptionByKey(turnContext, eventData.BusinessRuleName, cancellationToken);

                        // Get list of Conversations to update from SubscriptionManager
                        if (proactiveSubscriptions != null)
                        {
                            foreach (var proactiveSubscription in proactiveSubscriptions.ConversationReferences)
                            {
                                // Send adaptive notificaiton cards
                                await turnContext.Adapter.ContinueConversationAsync(_botSettings.MicrosoftAppId, proactiveSubscription, ContinueConversationCallback(turnContext, eventData, proactiveSubscription), cancellationToken);
                            }
                        }

                        break;
                    }

                default:
                    {
                        await _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
                        break;
                    }
            }
        }

        protected virtual Task<InvokeResponse> OnSigninVerifyStateAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            return Task.FromResult<InvokeResponse>(new InvokeResponse
            {
                Status = (int)HttpStatusCode.OK,
                Body = null
            });
        }

        protected override async Task<InvokeResponse> OnInvokeActivityAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            switch (turnContext.Activity.Name)
            {
                case "signin/verifyState":
                    await _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
                    return await OnSigninVerifyStateAsync(turnContext, cancellationToken);

                default:

                    var itsmTeamsActivityHandler = new ITSMTeamsInvokeActivityHandlerFactory(_serviceProvider);
                    var taskModuleContinueResponse = await itsmTeamsActivityHandler.HandleTaskModuleActivity(turnContext, cancellationToken);

                    return new InvokeResponse()
                    {
                        Status = (int)HttpStatusCode.OK,
                        Body = new TaskModuleResponse()
                        {
                            Task = taskModuleContinueResponse
                        }
                    };
            }
        }

        protected override Task OnEndOfConversationActivityAsync(ITurnContext<IEndOfConversationActivity> turnContext, CancellationToken cancellationToken)
        {
            return _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
        }

        /// <summary>
        /// Continue the conversation callback.
        /// </summary>
        /// <param name="turnContext">Turn context.</param>
        /// <param name="notification">Activity text.</param>
        /// <returns>Bot Callback Handler.</returns>
        private BotCallbackHandler ContinueConversationCallback(ITurnContext turnContext, ServiceNowNotification notification, ConversationReference conversationReference)
        {
            // Update Conversation
            return async (turnContext, cancellationToken) =>
            {
                var activity = turnContext.Activity.CreateReply();
                activity.Attachments = new List<Attachment>
                {
                    new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = notification.ToAdaptiveCard()
                    }
                };
                EnsureActivity(activity);

                if (turnContext.Activity.ChannelId == Microsoft.Bot.Connector.Channels.Msteams)
                {
                    // Get Activity ReferenceMap from Proactive State
                    ActivityReferenceMap activityReferenceMap = await _activityReferenceMapAccessor.GetAsync(
                turnContext,
                () => new ActivityReferenceMap(),
                cancellationToken)
                .ConfigureAwait(false);

                    // Return Added Incident Envelope
                    // Get saved Activity Reference mapping to conversation Id
                    activityReferenceMap.TryGetValue(conversationReference.Conversation.Id + notification.Id, out var activityReference);

                    // if there is no activity mapping to conversation reference
                    // then send a new activity and save activity to activityReferenceMap
                    if (activityReference == null)
                    {
                        var resourceResponse = await turnContext.SendActivityAsync(activity);

                        // Store Activity and Thread Id mapping to ConversationReference and TicketId from eventData
                        activityReferenceMap[conversationReference.Conversation.Id + notification.Id] = new ActivityReference
                        {
                            ActivityId = resourceResponse.Id,
                            ThreadId = conversationReference.Conversation.Id,
                        };
                    }
                    else
                    {
                        // Update Create Ticket Button with another Adaptive card to Update/Delete Ticket
                        await _teamsTicketUpdateActivity.UpdateTaskModuleActivityAsync(
                            turnContext,
                            activityReference,
                            notification.ToAdaptiveCard(),
                            cancellationToken);
                    }

                    // Save activity reference map state
                    await _activityReferenceMapAccessor.SetAsync(turnContext, activityReferenceMap).ConfigureAwait(false);

                    // Save Conversation State
                    await _proactiveState
                        .SaveChangesAsync(turnContext).ConfigureAwait(false);
                }
                else
                {
                    // Not a TeamsChannel just send a conversation update
                    await turnContext.SendActivityAsync(activity);
                }
            };
        }

        /// <summary>
        /// This method is required for proactive notifications to work in Web Chat.
        /// </summary>
        /// <param name="activity">Proactive Activity.</param>
        private void EnsureActivity(IMessageActivity activity)
        {
            if (activity != null)
            {
                if (activity.From != null)
                {
                    activity.From.Name = "User";
                    activity.From.Properties["role"] = "user";
                }

                if (activity.Recipient != null)
                {
                    activity.Recipient.Id = "1";
                    activity.Recipient.Name = "Bot";
                    activity.Recipient.Properties["role"] = "bot";
                }
            }
        }
    }
}
