// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;

namespace MusicSkill.Services
{
    public class SpotifyService : IMusicService
    {
        private SpotifyWebAPI client;

        public SpotifyService(BotSettings settings)
        {
            CredentialsAuth auth = new CredentialsAuth(settings.SpotifyClientId, settings.SpotifyClientSecret);
            Token token = auth.GetToken().Result;
            client = new SpotifyWebAPI() { TokenType = token.TokenType, AccessToken = token.AccessToken };
        }

        public async Task<string> SearchMusicAsync(string searchQuery, List<string> genres = null)
        {
            // Search query in each field
            List<string> searchFields = new List<string>();

            // Normal query
            if (!string.IsNullOrEmpty(searchQuery))
            {
                searchFields.Add(searchQuery);
            }

            // Query filtered by genre
            if (genres?.Count != 0)
            {
                searchFields.Add(Field.Genre + string.Join("+", genres).Replace(" ", "+"));
            }

            string q = string.Join(" ", searchFields);

            var searchItems = await client.SearchItemsEscapedAsync(q, SearchType.All, 5);

            // If any results exist, get the first playlist, then artist/track/album
            if (searchItems.Playlists?.Total != 0)
            {
                return searchItems.Playlists.Items[0].Uri;
            }
            else if (searchItems.Artists?.Total != 0)
            {
                return searchItems.Artists.Items[0].Uri;
            }
            else if (searchItems.Tracks?.Total != 0)
            {
                return searchItems.Tracks.Items[0].Uri;
            }
            else if (searchItems.Albums?.Total != 0)
            {
                return searchItems.Albums.Items[0].Uri;
            }

            return null;
        }

        public async Task<string> GetNewAlbumReleasesAsync()
        {
            // Get newly released album
            var newAlbumReleases = await client.GetNewAlbumReleasesAsync();

            if (newAlbumReleases.Albums.Items.Any())
            {
                return newAlbumReleases.Albums.Items.First().Uri;
            }

            return null;
        }

        private static class Field
        {
            public const string Genre = "genre:";
        }
    }
}
