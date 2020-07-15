// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards;
using ITSMSkill.Dialogs.Teams.SubscriptionTaskModule;
using ITSMSkill.Models.UpdateActivity;
using ITSMSkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace ITSMSkill.Dialogs
{
    /// <summary>
    /// Dialog class for for Creating BusinessRule Subscription.
    /// </summary>
    public class CreateSubscriptionDialog : SkillDialogBase
    {
        private readonly IStatePropertyAccessor<ActivityReferenceMap> _activityReferenceMapAccessor;
        private readonly ConversationState _conversationState;
        private readonly BotSettings _settings;

        public CreateSubscriptionDialog(
             IServiceProvider serviceProvider)
            : base(nameof(CreateSubscriptionDialog), serviceProvider)
        {
            _conversationState = serviceProvider.GetService<ConversationState>();
            _activityReferenceMapAccessor = _conversationState.CreateProperty<ActivityReferenceMap>(nameof(ActivityReferenceMap));
            _settings = serviceProvider.GetService<BotSettings>();

            // TaskModule Based WaterFallStep
            var createSubscriptionTaskModule = new WaterfallStep[]
            {
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                CreateSubscriptionTeamsTaskModuleAsync
            };

            AddDialog(new WaterfallDialog(Actions.CreateSubscriptionTaskModule, createSubscriptionTaskModule));
        }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default)
        {
            // If Channel is MsTeams use Teams TaskModule
            if (dc.Context.Activity.ChannelId == Microsoft.Bot.Connector.Channels.Msteams)
            {
                return await dc.BeginDialogAsync(Actions.CreateSubscriptionTaskModule, options, cancellationToken);
            }
            else
            {
                await dc.Context.SendActivityAsync("Subscription Not Supported For This Channel");
                return await dc.EndDialogAsync(await CreateActionResultAsync(dc.Context, true, cancellationToken), cancellationToken);
            }
        }

        protected async Task<DialogTurnResult> CreateSubscriptionTeamsTaskModuleAsync(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Send Create Ticket TaskModule Card
            var reply = sc.Context.Activity.CreateReply();
            reply.Attachments = new List<Attachment>()
            {
                new Microsoft.Bot.Schema.Attachment() { ContentType = AdaptiveCard.ContentType, Content = SubscriptionTaskModuleAdaptiveCard.GetSubcriptionInputCard(_settings.MicrosoftAppId) }
            };

            // Get ActivityId for purpose of mapping
            ResourceResponse resourceResponse = await sc.Context.SendActivityAsync(reply, cancellationToken);

            ActivityReferenceMap activityReferenceMap = await _activityReferenceMapAccessor.GetAsync(
                sc.Context,
                () => new ActivityReferenceMap(),
                cancellationToken)
            .ConfigureAwait(false);

            // Store Activity and Thread Id
            activityReferenceMap[sc.Context.Activity.Conversation.Id] = new ActivityReference
            {
                ActivityId = resourceResponse.Id,
                ThreadId = sc.Context.Activity.Conversation.Id,
            };
            await _activityReferenceMapAccessor.SetAsync(sc.Context, activityReferenceMap).ConfigureAwait(false);

            // Save Conversation State
            await _conversationState
                .SaveChangesAsync(sc.Context);

            return await sc.EndDialogAsync();
        }
    }
}
