// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace MusicSkill.Services
{
    public interface IMusicService
    {
        public Task<string> SearchMusicAsync(string searchQuery, List<string> genres = null);

        public Task<string> GetNewAlbumReleasesAsync();
    }
}
