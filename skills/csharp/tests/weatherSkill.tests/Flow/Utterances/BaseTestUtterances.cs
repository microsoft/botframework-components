// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;

namespace WeatherSkill.Tests.Flow.Utterances
{
    public class BaseTestUtterances : Dictionary<string, WeatherSkillLuis>
    {
        public BaseTestUtterances()
        {
        }

        public static double TopIntentScore { get; } = 0.9;

        public WeatherSkillLuis GetLuisWithNoneIntent()
        {
            return GetWeatherSkillLuis();
        }

        protected WeatherSkillLuis GetWeatherSkillLuis(
            string userInput = null,
            WeatherSkillLuis.Intent intent = WeatherSkillLuis.Intent.None,
            GeographyV2[] geographyV2s = null)
        {
            var weatherSkillLuis = new WeatherSkillLuis();
            weatherSkillLuis.Text = userInput;
            weatherSkillLuis.Intents = new Dictionary<WeatherSkillLuis.Intent, IntentScore>();
            weatherSkillLuis.Intents.Add(intent, new IntentScore() { Score = TopIntentScore });
            weatherSkillLuis.Entities = new WeatherSkillLuis._Entities();
            weatherSkillLuis.Entities._instance = new WeatherSkillLuis._Entities._Instance();
            weatherSkillLuis.Entities.geographyV2 = geographyV2s;
            return weatherSkillLuis;
        }
    }
}
