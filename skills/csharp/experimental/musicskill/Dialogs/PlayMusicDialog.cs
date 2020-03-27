// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
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
        public PlayMusicDialog(
            IServiceProvider serviceProvider)
            : base(nameof(PlayMusicDialog), serviceProvider)
        {
            var sample = new WaterfallStep[]
            {
                GetAndSendMusicResultAsync,
            };

            AddDialog(new WaterfallDialog(nameof(PlayMusicDialog), sample));

            InitialDialogId = nameof(PlayMusicDialog);
        }

        private async Task<DialogTurnResult> GetAndSendMusicResultAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var state = await StateAccessor.GetAsync(stepContext.Context, cancellationToken: cancellationToken);

            var status = false;
            if (string.IsNullOrEmpty(state.Query))
            {
                await stepContext.Context.SendActivityAsync(LocaleTemplateManager.GenerateActivityForLocale(MainResponses.NoResultstMessage), cancellationToken);
            }
            else
            {
                // Extract query entity to search against Spotify for
                var searchQuery = state.Query;

                // Get music api client
                IMusicService musicService = ServiceManager.InitMusicService();

                // Search library
                var searchItems = await musicService.SearchMusicAsync(searchQuery);
                if (!string.IsNullOrEmpty(searchItems))
                {
                    status = true;
                    await SendOpenDefaultAppEventActivityAsync(stepContext, searchItems, cancellationToken);
                }
                else
                {
                    await stepContext.Context.SendActivityAsync(LocaleTemplateManager.GenerateActivityForLocale(MainResponses.NoResultstMessage), cancellationToken);
                }
            }

            // End dialog
            return await stepContext.EndDialogAsync(new Models.ActionInfos.ActionResult() { ActionSuccess = status }, cancellationToken);
        }

        private async Task SendOpenDefaultAppEventActivityAsync(WaterfallStepContext stepContext, string spotifyResultUri, CancellationToken cancellationToken)
        {
            var replyEvent = stepContext.Context.Activity.CreateReply();
            replyEvent.Type = ActivityTypes.Event;
            replyEvent.Name = "OpenDefaultApp";
            replyEvent.Value = new OpenDefaultApp() { MusicUri = spotifyResultUri };
            await stepContext.Context.SendActivityAsync(replyEvent, cancellationToken);
        }
    }
}
