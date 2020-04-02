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

        public async Task<string> SearchMusicAsync(string searchQuery)
        {
            // Search library
            var searchItems = await client.SearchItemsEscapedAsync(searchQuery, SearchType.All, 5);

            // If any results exist, get the first playlist, then artist result
            if (searchItems.Playlists?.Total != 0)
            {
                return searchItems.Playlists.Items[0].Uri;
            }
            else if (searchItems.Artists?.Total != 0)
            {
                return searchItems.Artists.Items[0].Uri;
            }

            return null;
        }
    }
}
