// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using RideshareSkill.Tests.Mocks;
using RideshareSkill.Tests.Utterances;

namespace RideshareSkill.Tests.Utilities
{
    public class SkillTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { SampleDialogUtterances.Trigger, CreateIntent(SampleDialogUtterances.Trigger, RideshareSkillLuis.Intent.Sample) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, RideshareSkillLuis.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static RideshareSkillLuis CreateIntent(string userInput, RideshareSkillLuis.Intent intent)
        {
            var result = new RideshareSkillLuis
            {
                Text = userInput,
                Intents = new Dictionary<RideshareSkillLuis.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new RideshareSkillLuis._Entities
            {
                _instance = new RideshareSkillLuis._Entities._Instance()
            };

            return result;
        }
    }
}
