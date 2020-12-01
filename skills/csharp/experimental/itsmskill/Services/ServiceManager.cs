// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ITSMSkill.Models;
using Microsoft.Bot.Schema;

namespace ITSMSkill.Services
{
    /// <summary>
    /// class for creating ServiceNow objects for CRUD Tickets and CRUD of BusinessRuleSubscription.
    /// </summary>
    public class ServiceManager : IServiceManager
    {
        // Create Tokens for creating/editing/deleting tickets
        public IITServiceManagement CreateManagement(BotSettings botSettings, TokenResponse tokenResponse, ServiceCache serviceCache)
        {
            if (tokenResponse.ConnectionName == "ServiceNow" && !string.IsNullOrEmpty(botSettings.ServiceNowUrl) && !string.IsNullOrEmpty(botSettings.ServiceNowGetUserId))
            {
                return new ServiceNow.Management(botSettings.ServiceNowUrl, tokenResponse.Token, botSettings.LimitSize, botSettings.ServiceNowGetUserId, serviceCache);
            }
            else
            {
                return null;
            }
        }

        // Create Tokens for creating/editing/deleting business rules and rest messages
        public IServiceNowBusinessRuleSubscription CreateManagementForSubscription(BotSettings botSettings, TokenResponse tokenResponse, ServiceCache serviceCache)
        {
            if (tokenResponse.ConnectionName == "ServiceNow" && !string.IsNullOrEmpty(botSettings.ServiceNowUrl) && !string.IsNullOrEmpty(botSettings.ServiceNowGetUserId))
            {
                return new ServiceNow.ServiceNowBusinessRuleSubscribption(botSettings.ServiceNowUrl, tokenResponse.Token, botSettings.LimitSize, botSettings.ServiceNowNamespaceId, serviceCache);
            }
            else
            {
                return null;
            }
        }
    }
}
