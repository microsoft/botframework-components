// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.BotFrameworkFunctionalTests.WaterfallHostBot.Skills;
using Microsoft.Extensions.Configuration;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallHostBot
{
    /// <summary>
    /// A helper class that loads Skills information from configuration.
    /// </summary>
    /// <remarks>
    /// This class loads the skill settings from config and casts them into derived types of <see cref="SkillDefinition"/>
    /// so we can render prompts with the skills and in their groups.
    /// </remarks>
    public class SkillsConfiguration
    {
        public SkillsConfiguration(IConfiguration configuration)
        {
            var section = configuration?.GetSection("BotFrameworkSkills");
            var skills = section?.Get<SkillDefinition[]>();
            if (skills != null)
            {
                foreach (var skill in skills)
                {
                    Skills.Add(skill.Id, CreateSkillDefinition(skill));
                }
            }

            var skillHostEndpoint = configuration?.GetValue<string>(nameof(SkillHostEndpoint));
            if (!string.IsNullOrWhiteSpace(skillHostEndpoint))
            {
                SkillHostEndpoint = new Uri(skillHostEndpoint);
            }
        }

        public Uri SkillHostEndpoint { get; }

        public Dictionary<string, SkillDefinition> Skills { get; } = new Dictionary<string, SkillDefinition>();

        private static SkillDefinition CreateSkillDefinition(SkillDefinition skill)
        {
            // Note: we hard code this for now, we should dynamically create instances based on the manifests.
            // For now, this code creates a strong typed version of the SkillDefinition based on the skill group
            // and copies the info from settings into it. 
            SkillDefinition skillDefinition;
            switch (skill.Group)
            {
                case "Echo":
                    skillDefinition = ObjectPath.Assign<EchoSkill>(new EchoSkill(), skill);
                    break;
                case "Waterfall":
                    skillDefinition = ObjectPath.Assign<WaterfallSkill>(new WaterfallSkill(), skill);
                    break;
                case "Teams":
                    skillDefinition = ObjectPath.Assign<TeamsSkill>(new TeamsSkill(), skill);
                    break;
                default:
                    throw new Exception($"Unable to find definition class for {skill.Id}.");
            }

            return skillDefinition;
        }
    }
}
