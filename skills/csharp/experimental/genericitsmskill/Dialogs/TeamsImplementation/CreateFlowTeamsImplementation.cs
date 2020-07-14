// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using GenericITSMSkill.Dialogs.Helper;
using GenericITSMSkill.Dialogs.Helper.AdaptiveCardHelper;
using GenericITSMSkill.Models;
using GenericITSMSkill.Services;
using GenericITSMSkill.Teams;
using GenericITSMSkill.Teams.Invoke;
using GenericITSMSkill.Teams.TaskModule;
using GenericITSMSkill.UpdateActivity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Bot.Solutions.Proactive;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace GenericITSMSkill.Dialogs.TeamsImplementation
{
    /// <summary>
    /// CreateTicketTeamsImplementation Handles OnFetch and OnSumbit Activity for TaskModules
    /// </summary>
    [TeamsInvoke(FlowType = nameof(TeamsFlowType.CreateFlow))]
    public class CreateFlowTeamsImplementation : ITeamsTaskModuleHandler<TaskModuleContinueResponse>
    {
        private readonly IConfiguration _config;
        private readonly IStatePropertyAccessor<SkillState> _stateAccessor;
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly ITeamsActivity<AdaptiveCard> _teamsTicketUpdateActivity;
        private readonly IStatePropertyAccessor<ActivityReferenceMap> _activityReferenceMapAccessor;
        private readonly IStatePropertyAccessor<ProactiveModel> _proactiveStateAccessor;
        private readonly ProactiveState _proactiveState;
        private readonly ConversationState _conversationState;
        private readonly BotSettings _settings;
        private readonly BotServices _services;

        public CreateFlowTeamsImplementation(
           IServiceProvider serviceProvider)
        {
            _config = serviceProvider.GetService<IConfiguration>();
            _conversationState = serviceProvider.GetService<ConversationState>();
            _settings = serviceProvider.GetService<BotSettings>();
            _services = serviceProvider.GetService<BotServices>();
            _stateAccessor = _conversationState.CreateProperty<SkillState>(nameof(SkillState));
            _dataProtectionProvider = serviceProvider.GetService<IDataProtectionProvider>();
            _teamsTicketUpdateActivity = serviceProvider.GetService<ITeamsActivity<AdaptiveCard>>();
            _activityReferenceMapAccessor = _conversationState.CreateProperty<ActivityReferenceMap>(nameof(ActivityReferenceMap));
            _proactiveState = serviceProvider.GetService<ProactiveState>();
            _proactiveStateAccessor = _proactiveState.CreateProperty<ProactiveModel>(nameof(ProactiveModel));
        }

        public async Task<TaskModuleContinueResponse> OnTeamsTaskModuleFetchAsync(ITurnContext context, CancellationToken cancellationToken)
        {
            return new TaskModuleContinueResponse()
            {
                Value = new TaskModuleTaskInfo()
                {
                    Title = "GenericITSMSkill Flow",
                    Height = "medium",
                    Width = 500,
                    Card = new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = GenericITSMSkillAdaptiveCards.GetSubscriptionAdaptiveCard(_settings.MicrosoftAppId)
                    }
                }
            };
        }

        public async Task<TaskModuleContinueResponse> OnTeamsTaskModuleSubmitAsync(ITurnContext context, CancellationToken cancellationToken)
        {
            try
            {
                // Get ChannelId and substring to 10 characters
                var id = context.Activity.GetChannelData<TeamsChannelData>().Team.Id.Substring(0, 10);
                ActivityReferenceMap activityReferenceMap = await _activityReferenceMapAccessor.GetAsync(
                context,
                () => new ActivityReferenceMap(),
                cancellationToken)
                .ConfigureAwait(false);

                var state = await _stateAccessor.GetAsync(context, () => new SkillState());

                var activityValueObject = JObject.FromObject(context.Activity.Value);

                var isDataObject = activityValueObject.TryGetValue("data", StringComparison.InvariantCultureIgnoreCase, out JToken dataValue);
                JObject dataObject = null;
                if (isDataObject)
                {
                    dataObject = dataValue as JObject;

                    // Get FlowName
                    var flowName = dataObject.GetValue("FlowName");

                    // Get ServiceChoice
                    var serviceChoice = dataObject.GetValue("ServiceChoices");

                    string flowUrl = FlowUrlGeneratorHelper.GenerateUrl(
                        _dataProtectionProvider,
                        context.Activity.GetChannelData<TeamsChannelData>(),
                        _config["BotHostingURL"],
                        flowName.Value<string>(),
                        serviceChoice.Value<string>());

                    if (flowUrl != null)
                    {
                        // Return Added Incident Envelope
                        // Get saved Activity Reference mapping to conversation Id
                        activityReferenceMap.TryGetValue(context.Activity.Conversation.Id, out var activityReference);

                        var proactiveModel = await _proactiveStateAccessor.GetAsync(context, () => new ProactiveModel()).ConfigureAwait(false);
                        proactiveModel[id] = new ProactiveModel.ProactiveData
                        {
                            Conversation = context.Activity.GetConversationReference()
                        };

                        // Update Create FlowUrl Button with another Adaptive card to Update/Delete Ticket
                        await _teamsTicketUpdateActivity.UpdateTaskModuleActivityAsync(
                            context,
                            activityReference,
                            GenericITSMSkillAdaptiveCards.FlowUrlResponseCard("Please connect your flow to: " + flowUrl),
                            cancellationToken);

                        return new TaskModuleContinueResponse()
                        {
                            Type = "continue",
                            Value = new TaskModuleTaskInfo()
                            {
                                Title = "FlowUrl Created",
                                Height = "medium",
                                Width = 500,
                                Card = new Attachment
                                {
                                    ContentType = AdaptiveCard.ContentType,
                                    Content = GenericITSMSkillAdaptiveCards.FlowUrlResponseCard("FlowUrl Created")
                                }
                            }
                        };
                    }
                }

                // Failed to create FlowUrl
                return new TaskModuleContinueResponse()
                {
                    Type = "continue",
                    Value = new TaskModuleTaskInfo()
                    {
                        Title = "FlowUrl Create",
                        Height = "medium",
                        Width = 500,
                        Card = new Attachment
                        {
                            ContentType = AdaptiveCard.ContentType,
                            Content = GenericITSMSkillAdaptiveCards.FlowUrlResponseCard("Failed to Create FlowUrl")
                        }
                    }
                };
            }
            catch (Exception)
            {
                // Failed to create incident
                return new TaskModuleContinueResponse()
                {
                    Type = "continue",
                    Value = new TaskModuleTaskInfo()
                    {
                        Title = "FlowUrl Create",
                        Height = "medium",
                        Width = 500,
                        Card = new Attachment
                        {
                            ContentType = AdaptiveCard.ContentType,
                            Content = GenericITSMSkillAdaptiveCards.FlowUrlResponseCard("Failed to Create FlowUrl")
                        }
                    }
                };
            }
        }
    }
}
