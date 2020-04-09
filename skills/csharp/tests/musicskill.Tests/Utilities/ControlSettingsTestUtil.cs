// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using MusicSkill.Models;
using MusicSkill.Tests.Mocks;
using MusicSkill.Tests.Utterances;

namespace MusicSkill.Tests.Utilities
{
    public class ControlSettingsTestUtil : SkillTestUtilBase
    {
        private static Dictionary<string, IRecognizerConvert> _utterances;

        public static string PauseResult { get; } = $"{ControlSettingsUtterances.ActionHeader}.{ControlActions.Pause}";

        public static string ExcludeResult { get; } = $"{ControlSettingsUtterances.ActionHeader}.{ControlActions.Exclude}";

        public static string ShuffleResult { get; } = $"{ControlSettingsUtterances.ActionHeader}.{ControlActions.Shuffle}";

        public static string AdjustVolumeResult { get; } = $"{ControlSettingsUtterances.ActionHeader}.{ControlActions.AdjustVolume}";

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, MusicSkillLuis.Intent.None));
            InitializeUtterances();
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        private static void InitializeUtterances()
        {
            _utterances = new Dictionary<string, IRecognizerConvert>
            {
                {
                    ControlSettingsUtterances.Pause, CreateIntent(
                        ControlSettingsUtterances.Pause,
                        MusicSkillLuis.Intent.Pause)
                },
                {
                    ControlSettingsUtterances.Exclude, CreateIntent(
                        ControlSettingsUtterances.Exclude,
                        MusicSkillLuis.Intent.Exclude)
                },
                {
                    ControlSettingsUtterances.Shuffle, CreateIntent(
                        ControlSettingsUtterances.Shuffle,
                        MusicSkillLuis.Intent.Shuffle)
                },
                {
                    ControlSettingsUtterances.AdjustVolumn, CreateIntent(
                        ControlSettingsUtterances.AdjustVolumn,
                        MusicSkillLuis.Intent.AdjustVolume)
                },
                {
                    ControlSettingsUtterances.DefaultVolumnDirection, CreateIntent(
                        ControlSettingsUtterances.DefaultVolumnDirection,
                        MusicSkillLuis.Intent.AdjustVolume,
                        volumeDirection: new string[][] { new string[] { ControlSettingsUtterances.DefaultVolumnDirection } })
                },
                {
                    ControlSettingsUtterances.AdjustVolumnUp, CreateIntent(
                        ControlSettingsUtterances.AdjustVolumnUp,
                        MusicSkillLuis.Intent.AdjustVolume,
                        volumeDirection: new string[][] { new string[] { ControlSettingsUtterances.DefaultVolumnDirection } })
                },
                { PlayMusicDialogUtterances.None, CreateIntent(PlayMusicDialogUtterances.None, MusicSkillLuis.Intent.None) },
            };
        }
    }
}
