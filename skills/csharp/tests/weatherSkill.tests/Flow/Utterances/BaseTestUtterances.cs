// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;

namespace WeatherSkill.Tests.Flow.Utterances
{
    public class BaseTestUtterances : Dictionary<string, WeatherSkillLuis>
    {
        public BaseTestUtterances()
        {
        }

        public static double TopIntentScore { get; } = 0.9;

        public WeatherSkillLuis GetNoneIntent()
        {
            return GetWeatherIntent();
        }

        protected WeatherSkillLuis GetWeatherIntent(
            string userInput = null,
            WeatherSkillLuis.Intent intents = WeatherSkillLuis.Intent.None)
        {
            var intent = new WeatherSkillLuis();
            intent.Text = userInput;
            intent.Intents = new Dictionary<WeatherSkillLuis.Intent, IntentScore>();
            intent.Intents.Add(intents, new IntentScore() { Score = TopIntentScore });
            intent.Entities = new WeatherSkillLuis._Entities();
            intent.Entities._instance = new WeatherSkillLuis._Entities._Instance();

            return intent;
        }
    }
}
