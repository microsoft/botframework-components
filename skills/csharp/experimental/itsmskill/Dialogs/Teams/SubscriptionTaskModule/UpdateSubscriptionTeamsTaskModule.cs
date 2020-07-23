// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
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
using Newtonsoft.Json.Linq;

namespace ITSMSkill.Dialogs.Teams.SubscriptionTaskModule
{
    /// <summary>
    /// UpdateSubscriptionTaskModule Handles OnFetch and OnSumbit Activity for TaskModules
    /// </summary>
    [TeamsInvoke(FlowType = nameof(TeamsFlowType.UpdateSubscription_Form))]
    public class UpdateSubscriptionTeamsTaskModule : ITeamsTaskModuleHandler<TaskModuleContinueResponse>
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

        public UpdateSubscriptionTeamsTaskModule(IServiceProvider serviceProvider)
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
            var taskModuleMetadata = context.Activity.GetTaskModuleMetadata<TaskModuleMetadata>();

            var details = taskModuleMetadata.FlowData != null ?
                    JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(taskModuleMetadata.FlowData))
                    .GetValueOrDefault("SubscriptionDetails") : null;

            // Convert JObject to Ticket
            var subscription = JsonConvert.DeserializeObject<ServiceNowSubscription>(details.ToString());

            return new TaskModuleContinueResponse()
            {
                Value = new TaskModuleTaskInfo()
                {
                    Title = "Subscription",
                    Height = "medium",
                    Width = 500,
                    Card = new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = SubscriptionTaskModuleAdaptiveCard.PresentSubscriptionAdaptiveCard(subscription, _settings.MicrosoftAppId)
                    }
                }
            };
        }

        public async Task<TaskModuleContinueResponse> OnTeamsTaskModuleSubmitAsync(ITurnContext context, CancellationToken cancellationToken)
        {
            bool hasPropertyChanged = false;

            var state = await _stateAccessor.GetAsync(context, () => new SkillState());

            var activityValueObject = JObject.FromObject(context.Activity.Value);

            var taskModuleMetadata = context.Activity.GetTaskModuleMetadata<TaskModuleMetadata>();
            var details = taskModuleMetadata.FlowData != null ?
            JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(taskModuleMetadata.FlowData))
            .GetValueOrDefault("SubscriptionDetails") : null;

            var subscriptionDetails = JsonConvert.DeserializeObject<ServiceNowSubscription>(details.ToString());

            string filterName = subscriptionDetails.FilterName;

            var isDataObject = activityValueObject.TryGetValue("data", StringComparison.InvariantCultureIgnoreCase, out JToken dataValue);
            JObject dataObject = null;

            if (isDataObject)
            {
                dataObject = dataValue as JObject;

                string filterCondition = string.Empty;
                if (dataObject.GetValue("FilterCondition").Value<string>().Equals(string.Empty))
                {
                    filterCondition = subscriptionDetails.FilterCondition;
                }
                else
                {
                    filterCondition = dataObject.GetValue("FilterCondition").Value<string>();
                    hasPropertyChanged = true;
                }

                // Create FilterList from JTOKEN
                var conditions = IncidentSubscriptionStrings.FilterConditions.ToList<string>();

                ICollection<string> filterList = new List<string>();

                if (filterCondition.Contains("1"))
                {
                    filterList.Add(conditions[0]);
                }

                if (filterCondition.Contains("2"))
                {
                    filterList.Add(conditions[1]);
                }

                if (filterCondition.Contains("3"))
                {
                    filterList.Add(conditions[2]);
                }

                if (filterCondition.Contains("4"))
                {
                    filterList.Add(conditions[3]);
                }

                if (hasPropertyChanged)
                {
                    var management = _serviceManager.CreateManagementForSubscription(_settings, state.AccessTokenResponse, state.ServiceCache);

                    // Create Subscription New RESTAPI for callback from ServiceNow

                    // Create Subscription BusinessRule
                    var result = await management.UpdateSubscriptionBusinessRule(filterList, subscriptionDetails.FilterName);

                    if (result == System.Net.HttpStatusCode.OK)
                    {
                        var serviceNowSub = new ServiceNowSubscription
                        {
                            FilterName = subscriptionDetails.FilterName,
                            FilterCondition = filterCondition,
                            NotificationNameSpace = subscriptionDetails.NotificationNameSpace,
                            NotificationApiName = subscriptionDetails.NotificationApiName,
                        };
                        ActivityReferenceMap activityReferenceMap = await _activityReferenceMapAccessor.GetAsync(
                            context,
                            () => new ActivityReferenceMap(),
                            cancellationToken)
                        .ConfigureAwait(false);

                        // Return Added Incident Envelope
                        // Get saved Activity Reference mapping to conversation Id
                        activityReferenceMap.TryGetValue(context.Activity.Conversation.Id, out var activityReference);

                        // Update Create Ticket Button with another Adaptive card to Update/Delete Ticket
                        await _teamsTicketUpdateActivity.UpdateTaskModuleActivityAsync(
                            context,
                            activityReference,
                            SubscriptionTaskModuleAdaptiveCard.BuildSubscriptionAdaptiveCard(serviceNowSub, _settings.MicrosoftAppId),
                            cancellationToken);

                        return new TaskModuleContinueResponse()
                        {
                            Value = new TaskModuleTaskInfo()
                            {
                                Title = "Subscription",
                                Height = "medium",
                                Width = 500,
                                Card = new Attachment
                                {
                                    ContentType = AdaptiveCard.ContentType,
                                    Content = SubscriptionTaskModuleAdaptiveCard.SubscriptionResponseCard($"Updated Business RuleName: {filterName} in ServiceNow")
                                }
                            }
                        };
                    }
                }
            }

            return new TaskModuleContinueResponse()
            {
                Value = new TaskModuleTaskInfo()
                {
                    Title = "Subscription",
                    Height = "medium",
                    Width = 500,
                    Card = new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = SubscriptionTaskModuleAdaptiveCard.SubscriptionResponseCard($"Failed to update Business RuleName: {filterName} in ServiceNow")
                    }
                }
            };
        }
    }
}
