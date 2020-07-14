// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using GenericITSMSkill.Extensions;
using GenericITSMSkill.Models.ServiceDesk;
using GenericITSMSkill.Teams.Invoke;
using GenericITSMSkill.UpdateActivity;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Bot.Solutions.Proactive;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace GenericITSMSkill.Bots
{
    public class DefaultActivityHandler<T> : TeamsActivityHandler
        where T : Dialog
    {
        private readonly Dialog _dialog;
        private readonly BotState _conversationState;
        private readonly BotState _userState;
        private readonly ProactiveState _proactiveState;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly LocaleTemplateManager _templateEngine;
        private readonly IStatePropertyAccessor<ActivityReferenceMap> _activityReferenceMapAccessor;
        private readonly IStatePropertyAccessor<TicketIdCorrelationMap> _ticketIdCorrelationMapAccessor;
        private readonly IStatePropertyAccessor<ProactiveModel> _proactiveStateAccessor;
        private readonly MicrosoftAppCredentials _appCredentials;
        private readonly IConfiguration _configuration;
        private readonly IConnectorClient _connectorClient;
        private readonly IServiceProvider _serviceProvider;

        public DefaultActivityHandler(IServiceProvider serviceProvider, T dialog)
        {
            _dialog = dialog;
            _dialog.TelemetryClient = serviceProvider.GetService<IBotTelemetryClient>();
            _conversationState = serviceProvider.GetService<ConversationState>();
            _userState = serviceProvider.GetService<UserState>();
            _dialogStateAccessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));
            _templateEngine = serviceProvider.GetService<LocaleTemplateManager>();
            _activityReferenceMapAccessor = _conversationState.CreateProperty<ActivityReferenceMap>(nameof(ActivityReferenceMap));
            _ticketIdCorrelationMapAccessor = _conversationState.CreateProperty<TicketIdCorrelationMap>(nameof(TicketIdCorrelationMap));
            _configuration = serviceProvider.GetService<IConfiguration>();
            _connectorClient = serviceProvider.GetService<IConnectorClient>();
            _proactiveState = serviceProvider.GetService<ProactiveState>();
            _proactiveStateAccessor = _proactiveState.CreateProperty<ProactiveModel>(nameof(ProactiveModel));
            _activityReferenceMapAccessor = _conversationState.CreateProperty<ActivityReferenceMap>(nameof(ActivityReferenceMap));
            _serviceProvider = serviceProvider;
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
            await turnContext.SendActivityAsync(_templateEngine.GenerateActivityForLocale("IntroMessage"), cancellationToken);
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

            switch (ev.Name)
            {
                case "Proactive":
                    {
                        // Deserialize EventData
                        var eventData = JsonConvert.DeserializeObject<ServiceDeskNotification>(turnContext.Activity.Value.ToString());

                        // Get ProactiveModel from state
                        var proactiveModel = await _proactiveStateAccessor.GetAsync(turnContext, () => new ProactiveModel());

                        // Get Conversation Reference from ProactiveModel
                        var conversationReference = proactiveModel[eventData.ChannelId].Conversation;

                        // Send notification to activity
                        await turnContext.Adapter.ContinueConversationAsync(_configuration.GetSection(MicrosoftAppCredentials.MicrosoftAppIdKey)?.Value, conversationReference, ContinueConversationCallback(turnContext, eventData), cancellationToken);
                        break;
                    }

                default:
                    {
                        await _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
                        break;
                    }
            }
        }

        protected override async Task<InvokeResponse> OnInvokeActivityAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            var itsmTeamsActivityHandler = new FlowTaskModuleHandlerFactory(_serviceProvider);
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

        protected override Task OnEndOfConversationActivityAsync(ITurnContext<IEndOfConversationActivity> turnContext, CancellationToken cancellationToken)
        {
            return _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
        }

        private BotCallbackHandler ContinueConversationCallback(ITurnContext context, ServiceDeskNotification notification)
        {
            return async (turnContext, cancellationToken) =>
            {
                var reply = context.Activity.CreateReply();
                reply.Attachments = new List<Attachment>
                {
                    new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = notification.ToAdaptiveCard()
                    }
                };
                EnsureActivity(reply);

                // Get Activity mapping conversation id
                var activityMap = await _activityReferenceMapAccessor.GetAsync(turnContext, () => new ActivityReferenceMap());
                activityMap.TryGetValue(turnContext.Activity.Conversation.Id, out ActivityReference activityReference);

                // Get Old Activity and update it
                if (activityReference != null)
                {
                    // Perform in-place update
                    var teamsChannelActivity = reply.CreateConversationToTeamsChannel(
                             new TeamsChannelData
                             {
                                 Channel = new ChannelInfo(id: activityReference.ThreadId),
                             });

                    var response = await _connectorClient.Conversations.UpdateActivityAsync(
                      activityReference.ThreadId,
                      activityReference.ActivityId,
                      teamsChannelActivity,
                      cancellationToken);
                }
                else
                {
                    // Store Activity Id for in-place updates
                    var response = await turnContext.SendActivityAsync(reply).ConfigureAwait(false);
                    StoreActivityId(turnContext, response);
                }
            };
        }

        private async void StoreActivityId(ITurnContext turnContext, ResourceResponse resourceResponse)
        {
            ActivityReferenceMap activityReferenceMap = await _activityReferenceMapAccessor.GetAsync(
            turnContext,
            () => new ActivityReferenceMap(),
            CancellationToken.None)
            .ConfigureAwait(false);

            // Store Activity and Thread Id
            activityReferenceMap[turnContext.Activity.Conversation.Id] = new ActivityReference
            {
                ActivityId = resourceResponse.Id,
                ThreadId = turnContext.Activity.Conversation.Id,
                ConversationReference = turnContext.Activity.GetConversationReference()
            };
            await _activityReferenceMapAccessor.SetAsync(turnContext, activityReferenceMap).ConfigureAwait(false);

            // Save Conversation State
            await _conversationState
                .SaveChangesAsync(turnContext);
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
