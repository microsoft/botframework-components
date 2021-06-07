// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Extensions.Configuration;

namespace Microsoft.BotFrameworkFunctionalTests.SimpleHostBot
{
    /// <summary>
    /// A helper class that loads Skills information from configuration.
    /// </summary>
    public class SkillsConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SkillsConfiguration"/> class to load skills information from configuration.
        /// </summary>
        /// <param name="configuration">The configuration properties.</param>
        public SkillsConfiguration(IConfiguration configuration)
        {
            var section = configuration?.GetSection("BotFrameworkSkills");
            var skills = section?.Get<BotFrameworkSkill[]>();
            if (skills != null)
            {
                foreach (var skill in skills)
                {
                    Skills.Add(skill.Id, skill);
                }
            }

            var skillHostEndpoint = configuration?.GetValue<string>(nameof(SkillHostEndpoint));
            if (!string.IsNullOrWhiteSpace(skillHostEndpoint))
            {
                SkillHostEndpoint = new Uri(skillHostEndpoint);
            }
        }

        /// <summary>
        /// Gets the URI representing the endpoint of the host bot.
        /// </summary>
        /// <value>
        /// The URI representing the endpoint of the host bot.
        /// </value>
        public Uri SkillHostEndpoint { get; }

        /// <summary>
        /// Gets the key-value pairs with the skills bots.
        /// </summary>
        /// <value>
        /// The key-value pairs with the skills bots.
        /// </value>
        public Dictionary<string, BotFrameworkSkill> Skills { get; } = new Dictionary<string, BotFrameworkSkill>(StringComparer.OrdinalIgnoreCase);
    }
}
