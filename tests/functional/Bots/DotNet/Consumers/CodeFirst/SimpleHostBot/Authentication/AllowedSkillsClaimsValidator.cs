// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Bot.Connector.Authentication;

namespace Microsoft.BotFrameworkFunctionalTests.SimpleHostBot.Authentication
{
    /// <summary>
    /// Sample claims validator that loads an allowed list from configuration if present
    /// and checks that responses are coming from configured skills.
    /// </summary>
    public class AllowedSkillsClaimsValidator : ClaimsValidator
    {
        private readonly List<string> _allowedSkills;

        /// <summary>
        /// Initializes a new instance of the <see cref="AllowedSkillsClaimsValidator"/> class.
        /// Loads the appIds for the configured skills. Only allows responses from skills it has configured.
        /// </summary>
        /// <param name="skillsConfig">The list of configured skills.</param>
        public AllowedSkillsClaimsValidator(SkillsConfiguration skillsConfig)
        {
            if (skillsConfig == null)
            {
                throw new ArgumentNullException(nameof(skillsConfig));
            }

            _allowedSkills = (from skill in skillsConfig.Skills.Values select skill.AppId).ToList();
        }

        /// <summary>
        /// Checks that the appId claim in the skill request is in the list of skills configured for this bot.
        /// </summary>
        /// <param name="claims">The list of claims to validate.</param>
        /// <returns>A task that represents the work queued to execute.</returns>
        public override Task ValidateClaimsAsync(IList<Claim> claims)
        {
            if (SkillValidation.IsSkillClaim(claims))
            {
                var appId = JwtTokenValidation.GetAppIdFromClaims(claims);
                if (!_allowedSkills.Contains(appId))
                {
                    throw new UnauthorizedAccessException($"Received a request from an application with an appID of \"{appId}\". To enable requests from this skill, add the skill to your configuration file.");
                }
            }

            return Task.CompletedTask;
        }
    }
}
