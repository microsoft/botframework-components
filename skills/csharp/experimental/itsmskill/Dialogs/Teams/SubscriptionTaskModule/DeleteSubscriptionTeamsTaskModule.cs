// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using ITSMSkill.Extensions;
using ITSMSkill.Extensions.Teams;
using ITSMSkill.Extensions.Teams.TaskModule;
using ITSMSkill.Models;
using ITSMSkill.Models.ServiceNow;
using ITSMSkill.Models.UpdateActivity;
using ITSMSkill.Proactive.Subscription;
using ITSMSkill.Services;
using ITSMSkill.TeamsChannels;
using ITSMSkill.TeamsChannels.Invoke;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Bot.Solutions.Proactive;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace ITSMSkill.Dialogs.Teams.SubscriptionTaskModule
{
    /// <summary>
    /// DeleteSubscriptionTaskModule Handles OnFetch and OnSumbit Activity for TaskModules
    /// </summary>
    [TeamsInvoke(FlowType = nameof(TeamsFlowType.DeleteSubscription_Form))]
    public class DeleteSubscriptionTeamsTaskModule : ITeamsTaskModuleHandler<TaskModuleContinueResponse>
    {
        private readonly IStatePropertyAccessor<SkillState> _stateAccessor;
        private readonly IStatePropertyAccessor<ActivityReferenceMap> _activityReferenceMapAccessor;
        private readonly IStatePropertyAccessor<ActivityReferenceMap> _proactiveStateActivityReferenceMapAccessor;
        private readonly IStatePropertyAccessor<ConversationReferenceMap> _proactiveStateConversationReferenceMapAccessor;
        private readonly ConversationState _conversationState;
        private readonly BotSettings _settings;
        private readonly BotServices _services;
        private readonly IServiceManager _serviceManager;
        private readonly IConnectorClient _connectorClient;
        private readonly ITeamsActivity<AdaptiveCard> _teamsTicketUpdateActivity;
        private readonly IStatePropertyAccessor<ProactiveModel> _proactiveStateAccessor;
        private readonly ProactiveState _proactiveState;
        private readonly SubscriptionManager _subscriptionManager;

        public DeleteSubscriptionTeamsTaskModule(IServiceProvider serviceProvider)
        {
            _conversationState = serviceProvider.GetService<ConversationState>();
            _settings = serviceProvider.GetService<BotSettings>();
            _services = serviceProvider.GetService<BotServices>();
            _stateAccessor = _conversationState.CreateProperty<SkillState>(nameof(SkillState));
            _serviceManager = serviceProvider.GetService<IServiceManager>();
            _connectorClient = serviceProvider.GetService<IConnectorClient>();
            _teamsTicketUpdateActivity = serviceProvider.GetService<ITeamsActivity<AdaptiveCard>>();
            _proactiveState = serviceProvider.GetService<ProactiveState>();
            _proactiveStateAccessor = _proactiveState.CreateProperty<ProactiveModel>(nameof(ProactiveModel));
            _activityReferenceMapAccessor = _conversationState.CreateProperty<ActivityReferenceMap>(nameof(ActivityReferenceMap));
            _proactiveStateActivityReferenceMapAccessor = _proactiveState.CreateProperty<ActivityReferenceMap>(nameof(ActivityReferenceMap));
            _proactiveStateConversationReferenceMapAccessor = _proactiveState.CreateProperty<ConversationReferenceMap>(nameof(ConversationReferenceMap));
            _subscriptionManager = serviceProvider.GetService<SubscriptionManager>();
        }

        public async Task<TaskModuleContinueResponse> OnTeamsTaskModuleFetchAsync(ITurnContext context, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(context, () => new SkillState());

            var taskModuleMetadata = context.Activity.GetTaskModuleMetadata<TaskModuleMetadata>();

            var details = taskModuleMetadata.FlowData != null ?
                    JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(taskModuleMetadata.FlowData))
                    .GetValueOrDefault("SubscriptionDetails") : null;

            // Convert JObject to Ticket
            var subscription = JsonConvert.DeserializeObject<ServiceNowSubscription>(details.ToString());

            var management = _serviceManager.CreateManagementForSubscription(_settings, state.AccessTokenResponse, state.ServiceCache);

            // Create Subscription New RESTAPI for callback from ServiceNow

            // Create Subscription BusinessRule
            var result = await management.RemoveSubscriptionBusinessRule(subscription.FilterName);

            if (result == System.Net.HttpStatusCode.OK)
            {
                return new TaskModuleContinueResponse()
                {
                    Value = new TaskModuleTaskInfo()
                    {
                        Title = "Delete Subscription",
                        Height = "medium",
                        Width = 500,
                        Card = new Attachment
                        {
                            ContentType = AdaptiveCard.ContentType,
                            Content = SubscriptionTaskModuleAdaptiveCard.SubscriptionResponseCard($"Deleted Business RuleName: {subscription.FilterName} in ServiceNow")
                        }
                    }
                };
            }

            return new TaskModuleContinueResponse()
            {
                Value = new TaskModuleTaskInfo()
                {
                    Title = "Delete Subscription",
                    Height = "medium",
                    Width = 500,
                    Card = new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = SubscriptionTaskModuleAdaptiveCard.SubscriptionResponseCard($"Failed To Delete Business RuleName: {subscription.FilterName} in ServiceNow")
                    }
                }
            };
        }

        public Task<TaskModuleContinueResponse> OnTeamsTaskModuleSubmitAsync(ITurnContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
