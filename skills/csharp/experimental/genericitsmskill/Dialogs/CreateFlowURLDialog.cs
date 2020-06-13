// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using AdaptiveCards;
using GenericITSMSkill.Dialogs.Helper;
using GenericITSMSkill.Models;
using GenericITSMSkill.UpdateActivity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Bot.Solutions.Proactive;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace GenericITSMSkill.Dialogs
{
    public class CreateFlowURLDialog : SkillDialogBase
    {
        private readonly IConfiguration _config;
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly IStatePropertyAccessor<ActivityReferenceMap> _activityReferenceMapAccessor;
        private readonly IStatePropertyAccessor<TicketIdCorrelationMap> _ticketIdCorrelationMapAccessor;
        private readonly IStatePropertyAccessor<ProactiveModel> _proactiveStateAccessor;
        private readonly ProactiveState _proactiveState;
        private readonly ConversationState _conversationState;

        public CreateFlowURLDialog(
            IServiceProvider serviceProvider)
            : base(nameof(CreateFlowURLDialog), serviceProvider)
        {
            _config = serviceProvider.GetService<IConfiguration>();
            _dataProtectionProvider = serviceProvider.GetService<IDataProtectionProvider>();
            _conversationState = serviceProvider.GetService<ConversationState>();
            _activityReferenceMapAccessor = _conversationState.CreateProperty<ActivityReferenceMap>(nameof(ActivityReferenceMap));
            _ticketIdCorrelationMapAccessor = _conversationState.CreateProperty<TicketIdCorrelationMap>(nameof(TicketIdCorrelationMap));
            _proactiveState = serviceProvider.GetService<ProactiveState>();
            _proactiveStateAccessor = _proactiveState.CreateProperty<ProactiveModel>(nameof(ProactiveModel));

            var createFlow = new WaterfallStep[]
            {
                GetFlowInputAsync,
                CreateFlowUrlAsync,
                EndAsync,
            };

            AddDialog(new WaterfallDialog(nameof(CreateFlowURLDialog), createFlow));
            AddDialog(new TextPrompt(DialogIds.NamePrompt));

            InitialDialogId = nameof(CreateFlowURLDialog);
        }

        private async Task<DialogTurnResult> GetFlowInputAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            // NOTE: Uncomment the following lines to access LUIS result for this turn.
            // var luisResult = stepContext.Context.TurnState.Get<LuisResult>(StateProperties.SkillLuisResult);
            // Get Input from User
            var attachmentCard = GetFlowUrlInput.GetTicketIdInput();
            var cardReply = MessageFactory.Attachment(attachmentCard);

            // Get ResourceResponse of Sending Reply
            ResourceResponse resourceResponse = await sc.Context.SendActivityAsync(cardReply);

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
                ConversationReference = sc.Context.Activity.GetConversationReference()
            };
            await _activityReferenceMapAccessor.SetAsync(sc.Context, activityReferenceMap).ConfigureAwait(false);

            // Save Conversation State
            await _conversationState
                .SaveChangesAsync(sc.Context);

            return await sc.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> CreateFlowUrlAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var result = sc.Result as string;

            var endPointReply = new StringBuilder("Please connect your flow to: ");
            var hostingURL = _config["BotHostingURL"];
            endPointReply.Append(hostingURL);
            endPointReply.Append("/flow/messages/");

            // encrypt the channelID.
            var protector = _dataProtectionProvider.CreateProtector("test");
            TeamsChannelData channelData = sc.Context.Activity.GetChannelData<TeamsChannelData>();
            string channelID = channelData.Team.Id;

            var id = channelID.Substring(0, 10);
            string protectedChannelID = protector.Protect(id);
            endPointReply.Append(protectedChannelID);
            var endpointReplyActivity = MessageFactory.Text(endPointReply.ToString());

            // Send Activity
            await sc.Context.SendActivityAsync(endpointReplyActivity, cancellationToken);

            var proactiveModel = await _proactiveStateAccessor.GetAsync(sc.Context, () => new ProactiveModel()).ConfigureAwait(false);

            proactiveModel[id] = new ProactiveModel.ProactiveData
            {
                Conversation = sc.Context.Activity.GetConversationReference()
            };

            await _proactiveStateAccessor.SetAsync(sc.Context, proactiveModel).ConfigureAwait(false);

            // Save Conversation State
            await _conversationState
                .SaveChangesAsync(sc.Context, false, cancellationToken);
            await _proactiveState.SaveChangesAsync(sc.Context, false, cancellationToken);

            return await sc.NextAsync(cancellationToken: cancellationToken);
        }

        private Task<DialogTurnResult> EndAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private static class DialogIds
        {
            public const string NamePrompt = "namePrompt";
        }
    }
}
