// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace MusicSkill.Services
{
    public class ServiceManager : IServiceManager
    {
        private BotSettings _settings;

        public ServiceManager(BotSettings settings)
        {
            _settings = settings;
        }

        public IMusicService InitMusicService()
        {
            IMusicService musicService = new SpotifyService(_settings);

            return musicService;
        }
    }
}