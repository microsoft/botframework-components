// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Bot.Components.TokenExchangeSkillHandler
{
    internal class SkillsConfiguration
    {
        public SkillsConfiguration(IConfiguration configuration)
        {
            var section = configuration?.GetSection("skill");
            foreach (var child in section.GetChildren())
            {
                var props = child.GetChildren().ToDictionary(x => x.Key, x => x.Value);

                Skills.Add(child.Key, new BotFrameworkSkill()
                {
                    Id = child.Key,
                    AppId = props["msAppId"],
                    SkillEndpoint = new Uri(props["endpointUrl"]),
                });
            }

            SkillHostEndpoint = new Uri(configuration.GetValue<string>("skillHostEndpoint"));
        }

        public Uri SkillHostEndpoint { get; }

        public Dictionary<string, BotFrameworkSkill> Skills { get; } = new Dictionary<string, BotFrameworkSkill>();
    }
}
