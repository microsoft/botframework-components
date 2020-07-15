// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using ITSMSkill.Dialogs.Teams.CreateTicketTaskModuleView;
using ITSMSkill.Extensions.Teams;
using ITSMSkill.Models;
using ITSMSkill.Models.UpdateActivity;
using ITSMSkill.Services;
using ITSMSkill.TeamsChannels;
using ITSMSkill.TeamsChannels.Invoke;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json.Linq;

namespace ITSMSkill.Dialogs.Teams.TicketTaskModule
{
    /// <summary>
    /// CreateTicketTeamsImplementation Handles OnFetch and OnSumbit Activity for TaskModules
    /// </summary>
    [TeamsInvoke(FlowType = nameof(TeamsFlowType.CreateTicket_Form))]
    public class CreateTicketTeamsImplementation : ITeamsTaskModuleHandler<TaskModuleContinueResponse>
    {
        private readonly IStatePropertyAccessor<SkillState> _stateAccessor;
        private readonly IStatePropertyAccessor<ActivityReferenceMap> _activityReferenceMapAccessor;
        private readonly ConversationState _conversationState;
        private readonly BotSettings _settings;
        private readonly BotServices _services;
        private readonly IServiceManager _serviceManager;
        private readonly IConnectorClient _connectorClient;
        private readonly ITeamsActivity<AdaptiveCard> _teamsTicketUpdateActivity;

        public CreateTicketTeamsImplementation(
             IServiceProvider serviceProvider)
        {
            _conversationState = serviceProvider.GetService<ConversationState>();
            _settings = serviceProvider.GetService<BotSettings>();
            _services = serviceProvider.GetService<BotServices>();
            _stateAccessor = _conversationState.CreateProperty<SkillState>(nameof(SkillState));
            _serviceManager = serviceProvider.GetService<IServiceManager>();
            _activityReferenceMapAccessor = _conversationState.CreateProperty<ActivityReferenceMap>(nameof(ActivityReferenceMap));
            _connectorClient = serviceProvider.GetService<IConnectorClient>();
            _teamsTicketUpdateActivity = serviceProvider.GetService<ITeamsActivity<AdaptiveCard>>();
        }

        // Handle Fetch
        public async Task<TaskModuleContinueResponse> OnTeamsTaskModuleFetchAsync(ITurnContext context, CancellationToken cancellationToken)
        {
            return new TaskModuleContinueResponse()
            {
                Value = new TaskModuleTaskInfo()
                {
                    Title = "Create Incident",
                    Height = "medium",
                    Width = 500,
                    Card = new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = TicketDialogHelper.CreateIncidentAdaptiveCard(_settings.MicrosoftAppId)
                    }
                }
            };
        }

        // Handle Submit True
        public async Task<TaskModuleContinueResponse> OnTeamsTaskModuleSubmitAsync(ITurnContext context, CancellationToken cancellationToken)
        {
            var state = await _stateAccessor.GetAsync(context, () => new SkillState());

            ActivityReferenceMap activityReferenceMap = await _activityReferenceMapAccessor.GetAsync(
                context,
                () => new ActivityReferenceMap(),
                cancellationToken)
            .ConfigureAwait(false);

            var activityValueObject = JObject.FromObject(context.Activity.Value);

            var isDataObject = activityValueObject.TryGetValue("data", StringComparison.InvariantCultureIgnoreCase, out JToken dataValue);
            JObject dataObject = null;
            if (isDataObject)
            {
                dataObject = dataValue as JObject;

                // Get Title
                var title = dataObject.GetValue("IncidentTitle");

                // Get Description
                var description = dataObject.GetValue("IncidentDescription");

                // Get Urgency
                var urgency = dataObject.GetValue("IncidentUrgency");

                // Create Managemenet object
                var management = _serviceManager.CreateManagement(_settings, state.AccessTokenResponse, state.ServiceCache);

                // Create Ticket
                var result = await management.CreateTicket(title.Value<string>(), description.Value<string>(), (UrgencyLevel)Enum.Parse(typeof(UrgencyLevel), urgency.Value<string>(), true));
                if (result.Success)
                {
                    // Return Added Incident Envelope
                    // Get saved Activity Reference mapping to conversation Id
                    activityReferenceMap.TryGetValue(context.Activity.Conversation.Id, out var activityReference);

                    // Update Create Ticket Button with another Adaptive card to Update/Delete Ticket
                    await _teamsTicketUpdateActivity.UpdateTaskModuleActivityAsync(
                        context,
                        activityReference,
                        ServiceNowIncidentTaskModuleAdaptiveCardHelper.BuildIncidentCard(result.Tickets.FirstOrDefault(), _settings.MicrosoftAppId),
                        cancellationToken);

                    return new TaskModuleContinueResponse()
                    {
                        Type = "continue",
                        Value = new TaskModuleTaskInfo()
                        {
                            Title = "Incident Created",
                            Height = "medium",
                            Width = 500,
                            Card = new Attachment
                            {
                                ContentType = AdaptiveCard.ContentType,
                                Content = ServiceNowIncidentTaskModuleAdaptiveCardHelper.IncidentResponseCard("Incident has been created")
                            }
                        }
                    };
                }
            }

            // Failed to create incident
            return new TaskModuleContinueResponse()
            {
                Type = "continue",
                Value = new TaskModuleTaskInfo()
                {
                    Title = "Incident Create Failed",
                    Height = "medium",
                    Width = 500,
                    Card = new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = ServiceNowIncidentTaskModuleAdaptiveCardHelper.IncidentResponseCard("Incident Create Failed")
                    }
                }
            };
        }
    }
}
