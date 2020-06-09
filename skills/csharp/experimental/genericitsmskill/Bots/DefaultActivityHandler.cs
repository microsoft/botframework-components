// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using GenericITSMSkill.Extensions;
using GenericITSMSkill.Models.ServiceDesk;
using GenericITSMSkill.UpdateActivity;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace GenericITSMSkill.Bots
{
    public class DefaultActivityHandler<T> : ActivityHandler
        where T : Dialog
    {
        private readonly Dialog _dialog;
        private readonly BotState _conversationState;
        private readonly BotState _userState;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly LocaleTemplateManager _templateEngine;
        private readonly IStatePropertyAccessor<ActivityReferenceMap> _activityReferenceMapAccessor;
        private readonly IStatePropertyAccessor<TicketIdCorrelationMap> _ticketIdCorrelationMapAccessor;
        private readonly MicrosoftAppCredentials _appCredentials;

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
            _appCredentials = serviceProvider.GetService<MicrosoftAppCredentials>();
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
                        var eventData = JsonConvert.DeserializeObject<ServiceDeskNotification>(turnContext.Activity.Value.ToString());

                        TicketIdCorrelationMap ticketReferenceMap = await _ticketIdCorrelationMapAccessor.GetAsync(
                           turnContext,
                           () => new TicketIdCorrelationMap(),
                           cancellationToken)
                       .ConfigureAwait(false);

                        ticketReferenceMap.TryGetValue(eventData.ChannelId, out var ticketCorrelationId);

                        ActivityReferenceMap activityReferenceMap = await _activityReferenceMapAccessor.GetAsync(
                           turnContext,
                           () => new ActivityReferenceMap(),
                           cancellationToken)
                       .ConfigureAwait(false);

                        activityReferenceMap.TryGetValue(ticketCorrelationId.ThreadId, out var activityReference);

                        await turnContext.Adapter.ContinueConversationAsync(_appCredentials.MicrosoftAppId, activityReference.ConversationReference, ContinueConversationCallback(turnContext, eventData), cancellationToken);
                        break;
                    }

                default:
                    {
                        await _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
                        break;
                    }
            }
        }

        protected override Task OnEndOfConversationActivityAsync(ITurnContext<IEndOfConversationActivity> turnContext, CancellationToken cancellationToken)
        {
            return _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
        }

        private BotCallbackHandler ContinueConversationCallback(ITurnContext context, ServiceDeskNotification notification)
        {
            return async (turnContext, cancellationToken) =>
            {
                var activity = context.Activity.CreateReply();
                activity.Attachments = new List<Attachment>
                {
                    new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = notification.ToAdaptiveCard()
                    }
                };
                EnsureActivity(activity);
                await turnContext.SendActivityAsync(activity);
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
