// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace MusicSkill.Services
{
    public interface IServiceManager
    {
        IMusicService InitMusicService();
    }
}