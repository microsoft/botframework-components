// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using MusicSkill.Services;
using MusicSkill.Tests.Utterances;

namespace MusicSkill.Tests.Mocks
{
    public static class MockServiceManager
    {
        private static Mock<IMusicService> mockMusicService;
        private static Mock<IServiceManager> mockServiceManager;

        static MockServiceManager()
        {
            // music
            mockMusicService = new Mock<IMusicService>();
            mockMusicService.Setup(service => service.SearchMusicAsync(It.IsAny<string>(), It.IsAny<List<string>>())).Returns((string query, List<string> genres) =>
            {
                if (string.IsNullOrEmpty(query))
                {
                    return Task.FromResult(genres?.Count != 0 ? PlayMusicDialogUtterances.DefaultGenreUri : string.Empty);
                }

                if (query.Contains(PlayMusicDialogUtterances.DefaultArtist) && query.Contains(PlayMusicDialogUtterances.DefaultTrack))
                {
                    return Task.FromResult(PlayMusicDialogUtterances.DefaultArtistAndTrackUri);
                }
                else if (query.Contains(PlayMusicDialogUtterances.DefaultArtist) && genres?.Count != 0)
                {
                    return Task.FromResult(PlayMusicDialogUtterances.DefaultGenreAndArtistUri);
                }
                else if (query.Contains(PlayMusicDialogUtterances.DefaultPlayList))
                {
                    return Task.FromResult(PlayMusicDialogUtterances.DefaultPlayListUri);
                }
                else if (query.Contains(PlayMusicDialogUtterances.DefaultAlbum))
                {
                    return Task.FromResult(PlayMusicDialogUtterances.DefaultAlbumUri);
                }
                else if (query.Contains(PlayMusicDialogUtterances.DefaultArtist))
                {
                    return Task.FromResult(PlayMusicDialogUtterances.DefaultArtistUri);
                }
                else if (query.Contains(PlayMusicDialogUtterances.DefaultTrack))
                {
                    return Task.FromResult(PlayMusicDialogUtterances.DefaultTrackUri);
                }

                return Task.FromResult(string.Empty);
            });

            mockMusicService.Setup(service => service.GetNewAlbumReleasesAsync()).Returns(Task.FromResult(PlayMusicDialogUtterances.DefaultUri));

            // manager
            mockServiceManager = new Mock<IServiceManager>();
            mockServiceManager.Setup(manager => manager.InitMusicService()).Returns(mockMusicService.Object);
        }

        public static IServiceManager GetServiceManager()
        {
            return mockServiceManager.Object;
        }
    }
}
