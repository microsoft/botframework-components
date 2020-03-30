// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using MusicSkill.Tests.Mocks;
using MusicSkill.Tests.Utterances;

namespace MusicSkill.Tests.Utilities
{
    public class SkillTestUtil
    {
        private static Dictionary<string, IRecognizerConvert> _utterances = new Dictionary<string, IRecognizerConvert>
        {
            { PlayMusicDialogUtterances.PlayMusic, CreateIntent(PlayMusicDialogUtterances.PlayMusic, MusicSkillLuis.Intent.PlayMusic, searchInfo: new string[] { "music" }) },
            { PlayMusicDialogUtterances.None, CreateIntent(PlayMusicDialogUtterances.None, MusicSkillLuis.Intent.None) },
        };

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, MusicSkillLuis.Intent.None));
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        public static MusicSkillLuis CreateIntent(string userInput, MusicSkillLuis.Intent intent, string[] searchInfo = null)
        {
            var result = new MusicSkillLuis
            {
                Text = userInput,
                Intents = new Dictionary<MusicSkillLuis.Intent, IntentScore>()
            };

            result.Intents.Add(intent, new IntentScore() { Score = 0.9 });

            result.Entities = new MusicSkillLuis._Entities
            {
                _instance = new MusicSkillLuis._Entities._Instance(),
            };
            result.Entities.Artist_Any = searchInfo;

            return result;
        }
    }
}
