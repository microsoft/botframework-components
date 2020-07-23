// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ITSMSkill.Dialogs.Teams.TicketTaskModule
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveCards;
    using ITSMSkill.Dialogs.Teams.CreateTicketTaskModuleView;
    using ITSMSkill.Extensions;
    using ITSMSkill.Extensions.Teams;
    using ITSMSkill.Extensions.Teams.TaskModule;
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
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// UpdateTicket Handler for Updating Ticket OnFetch and OnSumbit Activity for TaskModules
    /// </summary>
    [TeamsInvoke(FlowType = nameof(TeamsFlowType.UpdateTicket_Form))]
    public class UpdateTicketTeamsTaskModule : ITeamsTaskModuleHandler<TaskModuleContinueResponse>
    {
        private readonly IStatePropertyAccessor<SkillState> _stateAccessor;
        private readonly ConversationState _conversationState;
        private readonly BotSettings _settings;
        private readonly BotServices _services;
        private readonly IServiceManager _serviceManager;
        private readonly IStatePropertyAccessor<ActivityReferenceMap> _activityReferenceMapAccessor;
        private readonly IConnectorClient _connectorClient;
        private readonly ITeamsActivity<AdaptiveCard> _teamsTicketUpdateActivity;

        public UpdateTicketTeamsTaskModule(IServiceProvider serviceProvider)
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

        public async Task<TaskModuleContinueResponse> OnTeamsTaskModuleFetchAsync(ITurnContext context, CancellationToken cancellationToken)
        {
            var taskModuleMetadata = context.Activity.GetTaskModuleMetadata<TaskModuleMetadata>();

            var ticketDetails = taskModuleMetadata.FlowData != null ?
                    JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(taskModuleMetadata.FlowData))
                    .GetValueOrDefault("IncidentDetails") : null;

            // Convert JObject to Ticket
            Ticket incidentDetails = JsonConvert.DeserializeObject<Ticket>(ticketDetails.ToString());

            return new TaskModuleContinueResponse()
            {
                Value = new TaskModuleTaskInfo()
                {
                    Title = "Update Incident",
                    Height = "medium",
                    Width = 500,
                    Card = new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = TicketDialogHelper.GetIncidentCard(incidentDetails, _settings.MicrosoftAppId)
                    }
                }
            };
        }

        public async Task<TaskModuleContinueResponse> OnTeamsTaskModuleSubmitAsync(ITurnContext context, CancellationToken cancellationToken)
        {
            bool hasPropertyChanged = false;

            ActivityReferenceMap activityReferenceMap = await _activityReferenceMapAccessor.GetAsync(
                context,
                () => new ActivityReferenceMap(),
                cancellationToken)
            .ConfigureAwait(false);

            var state = await _stateAccessor.GetAsync(context, () => new SkillState());

            var activityValueObject = JObject.FromObject(context.Activity.Value);

            var taskModuleMetadata = context.Activity.GetTaskModuleMetadata<TaskModuleMetadata>();
            var ticketDetails = taskModuleMetadata.FlowData != null ?
        JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(taskModuleMetadata.FlowData))
        .GetValueOrDefault("IncidentDetails") : null;

            // Convert JObject to Ticket
            Ticket incidentDetails = JsonConvert.DeserializeObject<Ticket>(ticketDetails.ToString());

            var isDataObject = activityValueObject.TryGetValue("data", StringComparison.InvariantCultureIgnoreCase, out JToken dataValue);
            JObject dataObject = null;

            if (isDataObject)
            {
                dataObject = dataValue as JObject;

                // Updated Properties
                string title = string.Empty;
                string description = string.Empty;
                UrgencyLevel urgency;

                if (dataObject.GetValue("IncidentTitle").Value<string>().Equals(string.Empty))
                {
                    title = incidentDetails.Title;
                }
                else
                {
                    title = dataObject.GetValue("IncidentTitle").Value<string>();
                    hasPropertyChanged = true;
                }

                if (dataObject.GetValue("IncidentDescription").Value<string>().Equals(string.Empty))
                {
                    description = incidentDetails.Description;
                }
                else
                {
                    description = dataObject.GetValue("IncidentDescription").Value<string>();
                    hasPropertyChanged = true;
                }

                if (dataObject.GetValue("IncidentUrgency").Value<string>().Equals(string.Empty))
                {
                    urgency = incidentDetails.Urgency;
                }
                else
                {
                    urgency = (UrgencyLevel)Enum.Parse(typeof(UrgencyLevel), dataObject.GetValue("IncidentUrgency").Value<string>(), true);
                    hasPropertyChanged = true;
                }

                //Create Managemenet object
                if (hasPropertyChanged)
                {
                    var management = _serviceManager.CreateManagement(_settings, state.AccessTokenResponse, state.ServiceCache);

                    // Create Ticket
                    var result = await management.UpdateTicket(incidentDetails.Id, title, description, urgency);

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
                                Title = "Incident Update",
                                Height = "medium",
                                Width = 500,
                                Card = new Attachment
                                {
                                    ContentType = AdaptiveCard.ContentType,
                                    Content = ServiceNowIncidentTaskModuleAdaptiveCardHelper.IncidentResponseCard("Incident has been updated")
                                }
                            }
                        };
                    }
                }

                return new TaskModuleContinueResponse()
                {
                    Type = "continue",
                    Value = new TaskModuleTaskInfo()
                    {
                        Title = "Incident Update",
                        Height = "medium",
                        Width = 500,
                        Card = new Attachment
                        {
                            ContentType = AdaptiveCard.ContentType,
                            Content = ServiceNowIncidentTaskModuleAdaptiveCardHelper.IncidentResponseCard("No Change Detected")
                        }
                    }
                };
            }

            // Failed to update incident
            return new TaskModuleContinueResponse()
            {
                Type = "continue",
                Value = new TaskModuleTaskInfo()
                {
                    Title = "Incident Update Failed",
                    Height = "medium",
                    Width = 500,
                    Card = new Attachment
                    {
                        ContentType = AdaptiveCard.ContentType,
                        Content = ServiceNowIncidentTaskModuleAdaptiveCardHelper.IncidentResponseCard("Incident Update Failed")
                    }
                }
            };
        }
    }
}
