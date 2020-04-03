// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using MusicSkill.Tests.Mocks;
using MusicSkill.Tests.Utterances;

namespace MusicSkill.Tests.Utilities
{
    public class PlayMusicTestUtil : SkillTestUtilBase
    {
        private static Dictionary<string, IRecognizerConvert> _utterances;

        public static string PlayMusicByAritist { get; } = $"Play {PlayMusicDialogUtterances.DefaultArtist}";

        public static string PlayMusicByPlayList { get; } = $"{PlayMusicDialogUtterances.DefaultBeforeMusic} {PlayMusicDialogUtterances.DefaultPlayList}";

        public static string PlayMusicByTrack { get; } = $"Play {PlayMusicDialogUtterances.DefaultTrack} {PlayMusicDialogUtterances.DefaultAfterMusic}";

        public static string PlayMusicByAlbum { get; } = $"{PlayMusicDialogUtterances.DefaultBeforeMusic} {PlayMusicDialogUtterances.DefaultAlbum} {PlayMusicDialogUtterances.DefaultAfterMusic}";

        public static string PlayMusicByTrackAndArtist { get; } = $"{PlayMusicDialogUtterances.DefaultBeforeMusic} {PlayMusicDialogUtterances.DefaultTrack} {PlayMusicDialogUtterances.DefaultInBetweenMusic} {PlayMusicDialogUtterances.DefaultArtist} {PlayMusicDialogUtterances.DefaultAfterMusic}";

        public static string PlayMusicByGenre { get; } = $"Play {PlayMusicDialogUtterances.DefaultGenre}";

        public static string PlayMusicByGenreAndArtist { get; } = $"Play {PlayMusicDialogUtterances.DefaultGenre} by {PlayMusicDialogUtterances.DefaultArtist}";

        public static MockLuisRecognizer CreateRecognizer()
        {
            var recognizer = new MockLuisRecognizer(defaultIntent: CreateIntent(string.Empty, MusicSkillLuis.Intent.None));
            InitializeUtterances();
            recognizer.RegisterUtterances(_utterances);
            return recognizer;
        }

        private static void InitializeUtterances()
        {
            var playMusicMusicParent = GetMusicParentClass(PlayMusicDialogUtterances.PlayMusic);

            var playMusicByAritistMusicParent = GetMusicParentClass(
                PlayMusicByAritist,
                music: new string[] { PlayMusicDialogUtterances.DefaultArtist });

            var playMusicByPlayListMusicParent = GetMusicParentClass(
                PlayMusicByPlayList,
                beforeMusic: new string[] { PlayMusicDialogUtterances.DefaultBeforeMusic },
                music: new string[] { PlayMusicDialogUtterances.DefaultPlayList });

            var playMusicByTrackMusicParent = GetMusicParentClass(
                PlayMusicByTrack,
                music: new string[] { PlayMusicDialogUtterances.DefaultTrack },
                afterMusic: new string[] { PlayMusicDialogUtterances.DefaultAfterMusic });

            var playMusicByAlbumMusicParent = GetMusicParentClass(
                PlayMusicByAlbum,
                beforeMusic: new string[] { PlayMusicDialogUtterances.DefaultBeforeMusic },
                music: new string[] { PlayMusicDialogUtterances.DefaultAlbum },
                afterMusic: new string[] { PlayMusicDialogUtterances.DefaultAfterMusic });

            var playMusicByTrackAndArtistMusicParent = GetMusicParentClass(
                PlayMusicByTrackAndArtist,
                beforeMusic: new string[] { PlayMusicDialogUtterances.DefaultBeforeMusic },
                music: new string[] { PlayMusicDialogUtterances.DefaultTrack, PlayMusicDialogUtterances.DefaultArtist },
                inBetweenMusic: new string[] { PlayMusicDialogUtterances.DefaultInBetweenMusic },
                afterMusic: new string[] { PlayMusicDialogUtterances.DefaultAfterMusic });

            var playMusicByGenreMusicParent = GetMusicParentClass(
                PlayMusicByGenre);

            var playMusicByGenreAndArtistMusicParent = GetMusicParentClass(
                PlayMusicByGenreAndArtist,
                music: new string[] { PlayMusicDialogUtterances.DefaultArtist });

            _utterances = new Dictionary<string, IRecognizerConvert>
            {
                {
                    PlayMusicDialogUtterances.PlayMusic, CreateIntent(
                        PlayMusicDialogUtterances.PlayMusic,
                        MusicSkillLuis.Intent.PlayMusic,
                        new MusicSkillLuis._Entities.MusicParentClass[] { playMusicMusicParent })
                },
                {
                    PlayMusicByAritist, CreateIntent(
                        PlayMusicByAritist,
                        MusicSkillLuis.Intent.PlayMusic,
                        new MusicSkillLuis._Entities.MusicParentClass[] { playMusicByAritistMusicParent })
                },
                {
                    PlayMusicByPlayList, CreateIntent(
                        PlayMusicByPlayList,
                        MusicSkillLuis.Intent.PlayMusic,
                        new MusicSkillLuis._Entities.MusicParentClass[] { playMusicByPlayListMusicParent })
                },
                {
                    PlayMusicByTrack, CreateIntent(
                        PlayMusicByTrack,
                        MusicSkillLuis.Intent.PlayMusic,
                        new MusicSkillLuis._Entities.MusicParentClass[] { playMusicByTrackMusicParent })
                },
                {
                    PlayMusicByAlbum, CreateIntent(
                        PlayMusicByAlbum,
                        MusicSkillLuis.Intent.PlayMusic,
                        new MusicSkillLuis._Entities.MusicParentClass[] { playMusicByAlbumMusicParent })
                },
                {
                    PlayMusicByTrackAndArtist, CreateIntent(
                        PlayMusicByTrackAndArtist,
                        MusicSkillLuis.Intent.PlayMusic,
                        new MusicSkillLuis._Entities.MusicParentClass[] { playMusicByTrackAndArtistMusicParent })
                },
                {
                    PlayMusicByGenre, CreateIntent(
                        PlayMusicByGenre,
                        MusicSkillLuis.Intent.PlayMusic,
                        new MusicSkillLuis._Entities.MusicParentClass[] { playMusicByGenreMusicParent },
                        genres: new string[] { PlayMusicDialogUtterances.DefaultGenre })
                },
                {
                    PlayMusicByGenreAndArtist, CreateIntent(
                        PlayMusicByGenreAndArtist,
                        MusicSkillLuis.Intent.PlayMusic,
                        new MusicSkillLuis._Entities.MusicParentClass[] { playMusicByGenreAndArtistMusicParent },
                        genres: new string[] { PlayMusicDialogUtterances.DefaultGenre })
                },
                { PlayMusicDialogUtterances.None, CreateIntent(PlayMusicDialogUtterances.None, MusicSkillLuis.Intent.None) },
            };
        }

        private static MusicSkillLuis._Entities.MusicParentClass GetMusicParentClass(
            string userInput,
            string[] beforeMusic = null,
            string[] music = null,
            string[] inBetweenMusic = null,
            string[] afterMusic = null,
            string[] genre = null)
        {
            return new MusicSkillLuis._Entities.MusicParentClass()
            {
                beforeMusic = beforeMusic,
                music = music,
                inBetweenMusic = inBetweenMusic,
                afterMusic = afterMusic,
                genre = genre,
                _instance = new MusicSkillLuis._Entities._InstanceMusicParent()
                {
                    beforeMusic = GetInstanceDatas(userInput, beforeMusic),
                    music = GetInstanceDatas(userInput, music),
                    inBetweenMusic = GetInstanceDatas(userInput, inBetweenMusic),
                    afterMusic = GetInstanceDatas(userInput, afterMusic),
                    genre = GetInstanceDatas(userInput, genre),
                }
            };
        }
    }
}
