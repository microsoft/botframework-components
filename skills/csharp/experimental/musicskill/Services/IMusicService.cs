// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading.Tasks;

namespace MusicSkill.Services
{
    public interface IMusicService
    {
        public Task<string> SearchMusic(string searchQuery);
    }
}
