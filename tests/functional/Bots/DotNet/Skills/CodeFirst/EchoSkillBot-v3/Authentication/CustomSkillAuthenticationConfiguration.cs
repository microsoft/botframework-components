﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Connector.SkillAuthentication;
using System.Configuration;
using System.Linq;

namespace Microsoft.BotFrameworkFunctionalTests.EchoSkillBotv3.Authentication
{
    public class CustomSkillAuthenticationConfiguration : AuthenticationConfiguration
    {
        private const string AllowedCallersConfigKey = "EchoBotAllowedCallers";
        public CustomSkillAuthenticationConfiguration()
        {
            // Could pull this list from a DB or anywhere.
            var allowedCallers = ConfigurationManager.AppSettings[AllowedCallersConfigKey].Split(',').Select(s => s.Trim().ToUpperInvariant()).ToList();
            ClaimsValidator = new CustomAllowedCallersClaimsValidator(allowedCallers);
        }
    }
}
