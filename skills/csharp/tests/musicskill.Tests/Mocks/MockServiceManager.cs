// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

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
            mockMusicService.Setup(service => service.SearchMusicAsync(It.IsAny<string>())).Returns(Task.FromResult(PlayMusicDialogUtterances.DefaultUri));

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
