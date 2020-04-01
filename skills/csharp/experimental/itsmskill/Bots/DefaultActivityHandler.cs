// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using ITSMSkill.Extensions;
using ITSMSkill.Extensions.Teams.TaskModule;
using ITSMSkill.Models.ServiceNow;
using ITSMSkill.Proactive;
using ITSMSkill.Responses.Main;
using ITSMSkill.Services;
using ITSMSkill.TeamsChannels.Invoke;
using ITSMSkill.Utilities;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.ApplicationInsights;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Proactive;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace ITSMSkill.Bots
{
    public class DefaultActivityHandler<T> : TeamsActivityHandler
        where T : Dialog
    {
        private readonly Dialog _dialog;
        private readonly BotState _conversationState;
        private readonly BotState _userState;
        private readonly ProactiveState _proactiveState;
        private readonly MicrosoftAppCredentials _appCredentials;
        private readonly IStatePropertyAccessor<DialogState> _dialogStateAccessor;
        private readonly IStatePropertyAccessor<ProactiveModel> _proactiveStateAccessor;
        private readonly LocaleTemplateManager _templateManager;
        private readonly BotSettings _botSettings;
        private readonly BotServices _botServices;
        private readonly IServiceManager _serviceManager;
        private readonly BotTelemetryClient _botTelemetryClient;

        public DefaultActivityHandler(IServiceProvider serviceProvider, T dialog)
        {
            _dialog = dialog;
            _dialog.TelemetryClient = serviceProvider.GetService<IBotTelemetryClient>();
            _conversationState = serviceProvider.GetService<ConversationState>();
            _userState = serviceProvider.GetService<UserState>();
            _proactiveState = serviceProvider.GetService<ProactiveState>();
            _dialogStateAccessor = _conversationState.CreateProperty<DialogState>(nameof(DialogState));
            _proactiveStateAccessor = _proactiveState.CreateProperty<ProactiveModel>(nameof(ProactiveModel));
            _appCredentials = serviceProvider.GetService<MicrosoftAppCredentials>();
            _templateManager = serviceProvider.GetService<LocaleTemplateManager>();
            _botSettings = serviceProvider.GetService<BotSettings>();
            _botServices = serviceProvider.GetService<BotServices>();
            _serviceManager = serviceProvider.GetService<ServiceManager>();
            _botTelemetryClient = serviceProvider.GetService<BotTelemetryClient>();
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
            return _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
        }

        protected override Task OnTeamsSigninVerifyStateAsync(ITurnContext<IInvokeActivity> turnContext, CancellationToken cancellationToken)
        {
            return _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
        }

        protected override async Task OnEventActivityAsync(ITurnContext<IEventActivity> turnContext, CancellationToken cancellationToken)
        {
            var ev = turnContext.Activity.AsEventActivity();
            var value = ev.Value?.ToString();

            switch (ev.Name)
            {
                case ServiceNowEvents.Proactive:
                    {
                        var eventData = JsonConvert.DeserializeObject<ServiceNowNotification>(turnContext.Activity.Value.ToString());

                        var proactiveModel = await _proactiveStateAccessor.GetAsync(turnContext, () => new ProactiveModel());

                        // TODO: Implement a proactive subscription manager for mapping Notification to ConversationReference
                        var conversationReference = proactiveModel["Key"].Conversation;

                        await turnContext.Adapter.ContinueConversationAsync(_appCredentials.MicrosoftAppId, conversationReference, ContinueConversationCallback(turnContext, eventData), cancellationToken);
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
            var itsmTeamsActivityHandler = new ITSMTeamsInvokeActivityHandlerFactory(_botSettings, _botServices, (ConversationState)_conversationState, _serviceManager, _botTelemetryClient);
            ITeamsInvokeEnvelope teamsInvokeEnvelope = await itsmTeamsActivityHandler.GetInvokeEnvelope(turnContext, cancellationToken);

            return new InvokeResponse()
            {
                Status = (int)HttpStatusCode.OK,
                Body = teamsInvokeEnvelope
            };
        }

        protected override Task OnEndOfConversationActivityAsync(ITurnContext<IEndOfConversationActivity> turnContext, CancellationToken cancellationToken)
        {
            return _dialog.RunAsync(turnContext, _dialogStateAccessor, cancellationToken);
        }

        /// <summary>
        /// Continue the conversation callback.
        /// </summary>
        /// <param name="context">Turn context.</param>
        /// <param name="message">Activity text.</param>
        /// <returns>Bot Callback Handler.</returns>
        private BotCallbackHandler ContinueConversationCallback(ITurnContext context, ServiceNowNotification notification)
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
