// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;

namespace WeatherSkill.Tests.Flow.Utterances
{
    public class GeneralTestUtterances : Dictionary<string, GeneralLuis>
    {
        public GeneralTestUtterances()
        {
        }

        public static double TopIntentScore { get; } = 0.9;

        public GeneralLuis GetLuisWithNoneIntent()
        {
            return GetGeneralLuis();
        }

        public GeneralLuis GetGeneralLuis(
            string userInput = null,
            GeneralLuis.Intent intent = GeneralLuis.Intent.None)
        {
            var generalLuis = new GeneralLuis();
            generalLuis.Text = userInput;
            generalLuis.Intents = new Dictionary<GeneralLuis.Intent, IntentScore>();
            generalLuis.Intents.Add(intent, new IntentScore() { Score = TopIntentScore });
            return generalLuis;
        }
    }
}
