// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using ITSMSkill.Dialogs.Teams.SubscriptionTaskModule;
using ITSMSkill.Dialogs.Teams.TicketTaskModule;
using ITSMSkill.Extensions.Teams;
using Microsoft.Bot.Schema.Teams;


namespace ITSMSkill.TeamsChannels.Invoke
{

    /// <summary>
    /// ITSMTeamsInvokeActivityhandler Factory Class for TaskModules
    /// </summary>
    public class ITSMTeamsInvokeActivityHandlerFactory : TeamsInvokeActivityHandlerFactory
    {
        public ITSMTeamsInvokeActivityHandlerFactory(IServiceProvider serviceProvider)
        {
            this.TaskModuleFetchSubmitMap = new Dictionary<string, Func<ITeamsTaskModuleHandler<TaskModuleContinueResponse>>>
            {
                {
                    $"{TeamsFlowType.CreateTicket_Form}",
                    () => new CreateTicketTeamsImplementation(serviceProvider)
                },
                {
                    $"{TeamsFlowType.UpdateTicket_Form}",
                    () => new UpdateTicketTeamsImplementation(serviceProvider)
                },
                {
                    $"{TeamsFlowType.DeleteTicket_Form}",
                    () => new DeleteTicketTeamsImplementation(serviceProvider)
                },
                {
                    $"{TeamsFlowType.CreateSubscription_Form}",
                    () => new CreateSubscriptionTeamsImplementation(serviceProvider)
                }
            };
        }
    }
}
