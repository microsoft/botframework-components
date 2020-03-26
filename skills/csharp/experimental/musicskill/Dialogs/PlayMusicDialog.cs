// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using MusicSkill.Models;
using MusicSkill.Responses.Main;
using MusicSkill.Services;

namespace MusicSkill.Dialogs
{
    public class PlayMusicDialog : SkillDialogBase
    {
        private LocaleTemplateManager _templateManager;

        public PlayMusicDialog(
            BotSettings settings,
            BotServices services,
            LocaleTemplateManager templateManager,
            ConversationState conversationState,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient)
            : base(nameof(PlayMusicDialog), settings, services, templateManager, conversationState, serviceManager, telemetryClient)
        {
            _templateManager = templateManager;

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
                await stepContext.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale(MainResponses.NoResultstMessage));
            }
            else
            {
                // Extract query entity to search against Spotify for
                var searchQuery = state.Query;

                // Get music api client
                IMusicService musicService = ServiceManager.InitMusicService();

                // Search library
                var searchItems = await musicService.SearchMusic(searchQuery);
                if (!string.IsNullOrEmpty(searchItems))
                {
                    status = true;
                    await SendOpenDefaultAppEventActivity(stepContext, searchItems, cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(_templateManager.GenerateActivityForLocale(MainResponses.NoResultstMessage));
                }
            }

            // End dialog
            return await stepContext.EndDialogAsync(new Models.ActionInfos.ActionResult() { ActionSuccess = status });
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
