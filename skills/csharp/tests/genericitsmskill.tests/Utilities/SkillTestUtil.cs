// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using GenericITSMSkill.Tests.Mocks;
using GenericITSMSkill.Tests.Utterances;

namespace GenericITSMSkill.Tests.Utilities
{
    public class SkillTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { SampleDialogUtterances.Trigger, CreateIntent(SampleDialogUtterances.Trigger, GenericITSMSkillLuis.Intent.Sample) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, GenericITSMSkillLuis.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static GenericITSMSkillLuis CreateIntent(string userInput, GenericITSMSkillLuis.Intent intent)
        {
            var result = new GenericITSMSkillLuis
            {
                Text = userInput,
                Intents = new Dictionary<GenericITSMSkillLuis.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new GenericITSMSkillLuis._Entities
            {
                _instance = new GenericITSMSkillLuis._Entities._Instance()
            };

            return result;
        }
    }
}
