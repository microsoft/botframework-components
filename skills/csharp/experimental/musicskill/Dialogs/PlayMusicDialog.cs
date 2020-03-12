// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using MusicSkill.Models;
using MusicSkill.Responses.Main;
using MusicSkill.Services;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web.Enums;
using SpotifyAPI.Web.Models;

namespace MusicSkill.Dialogs
{
    public class PlayMusicDialog : SkillDialogBase
    {
        private LocaleTemplateEngineManager _localeTemplateEngineManager;

        public PlayMusicDialog(
            BotSettings settings,
            BotServices services,
            LocaleTemplateEngineManager localeTemplateEngineManager,
            ConversationState conversationState,
            IBotTelemetryClient telemetryClient)
            : base(nameof(PlayMusicDialog), settings, services, localeTemplateEngineManager, conversationState, telemetryClient)
        {
            _localeTemplateEngineManager = localeTemplateEngineManager;

            var sample = new WaterfallStep[]
            {
                GetAndSendMusicResult,
            };

            AddDialog(new WaterfallDialog(nameof(PlayMusicDialog), sample));

            InitialDialogId = nameof(PlayMusicDialog);
        }

        private async Task<DialogTurnResult> GetAndSendMusicResult(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await StateAccessor.GetAsync(stepContext.Context);

            var status = false;
            if (string.IsNullOrEmpty(state.Query))
            {
                await stepContext.Context.SendActivityAsync(_localeTemplateEngineManager.GenerateActivityForLocale(MainResponses.NoResultstMessage));
            }
            else
            {
                // Extract query entity to search against Spotify for
                var searchQuery = state.Query;

                // Get music api client
                var client = await GetSpotifyWebAPIClient(Settings);

                // Search library
                var searchItems = await client.SearchItemsEscapedAsync(searchQuery, SearchType.All, 5);

                // If any results exist, get the first playlist, then artist result
                if (searchItems.Playlists?.Total != 0)
                {
                    status = true;
                    await SendOpenDefaultAppEventActivity(stepContext, searchItems.Playlists.Items[0].Uri, cancellationToken);
                }
                else if (searchItems.Artists?.Total != 0)
                {
                    status = true;
                    await SendOpenDefaultAppEventActivity(stepContext, searchItems.Artists.Items[0].Uri, cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(_localeTemplateEngineManager.GenerateActivityForLocale(MainResponses.NoResultstMessage));
                }
            }

            // End dialog
            return await stepContext.EndDialogAsync(new Models.ActionInfos.ActionResult() { ActionSuccess = status });
        }

        private async Task<SpotifyWebAPI> GetSpotifyWebAPIClient(BotSettings settings)
        {
            CredentialsAuth auth = new CredentialsAuth(settings.SpotifyClientId, settings.SpotifyClientSecret);
            Token token = await auth.GetToken();
            SpotifyWebAPI api = new SpotifyWebAPI() { TokenType = token.TokenType, AccessToken = token.AccessToken };
            return api;
        }

        private async Task SendOpenDefaultAppEventActivity(WaterfallStepContext stepContext, string spotifyResultUri, CancellationToken cancellationToken)
        {
            var replyEvent = stepContext.Context.Activity.CreateReply();
            replyEvent.Type = ActivityTypes.Event;
            replyEvent.Name = "OpenDefaultApp";
            replyEvent.Value = new OpenDefaultApp() { MusicUri = spotifyResultUri };
            await stepContext.Context.SendActivityAsync(replyEvent, cancellationToken);
        }
    }
}
