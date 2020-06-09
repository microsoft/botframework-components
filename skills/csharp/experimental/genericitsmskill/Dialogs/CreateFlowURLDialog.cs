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

            var sample = new WaterfallStep[]
            {
                GetAuthTokenAsync,
                AfterGetAuthTokenAsync,
                GetFlowInputAsync,
                CreateFlowUrlAsync,
                EndAsync,
            };

            AddDialog(new WaterfallDialog(nameof(CreateFlowURLDialog), sample));
            AddDialog(new TextPrompt(DialogIds.NamePrompt));

            InitialDialogId = nameof(CreateFlowURLDialog);
        }

        private async Task<DialogTurnResult> GetFlowInputAsync(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            // NOTE: Uncomment the following lines to access LUIS result for this turn.
            // var luisResult = stepContext.Context.TurnState.Get<LuisResult>(StateProperties.SkillLuisResult);
            MockData fakePayload = new MockData("1234", "This is Test", 3, "Active");
            var attachmentCard = GetFlowUrlInput.GetTicketIdInput("123", "123");
            var cardReply = MessageFactory.Attachment(attachmentCard);
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

            // Save Conversation State
            await _conversationState
                .SaveChangesAsync(sc.Context);

            var endPointReply = new StringBuilder("Please connect your flow to: ");
            var hostingURL = _config["BotHostingURL"];
            endPointReply.Append(hostingURL);
            endPointReply.Append("/flow/messages/");

            // encrypt the channelID.
            var protector = _dataProtectionProvider.CreateProtector("test");
            TeamsChannelData channelData = sc.Context.Activity.GetChannelData<TeamsChannelData>();
            string channelID = channelData.Team.Id;
            string protectedChannelID = protector.Protect(channelID);
            endPointReply.Append(protectedChannelID);
            var endpointReplyActivity = MessageFactory.Text(endPointReply.ToString());
            await sc.Context.SendActivityAsync(endpointReplyActivity, cancellationToken);

            TicketIdCorrelationMap ticketIdReferenceMap = await _ticketIdCorrelationMapAccessor.GetAsync(
             sc.Context,
             () => new TicketIdCorrelationMap(),
             cancellationToken)
         .ConfigureAwait(false);

            // Store Activity and Thread Id
            ticketIdReferenceMap[protectedChannelID] = new TicketIdCorrelation
            {
                TicketId = protectedChannelID,
                ThreadId = sc.Context.Activity.Conversation.Id,
            };
            await _ticketIdCorrelationMapAccessor.SetAsync(sc.Context, ticketIdReferenceMap).ConfigureAwait(false);

            return await sc.NextAsync(cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> GreetUserAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            dynamic data = new { Name = stepContext.Result.ToString() };
            var response = TemplateEngine.GenerateActivityForLocale("HaveNameMessage", data);
            await stepContext.Context.SendActivityAsync(response);

            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }

        private Task<DialogTurnResult> EndAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private static class DialogIds
        {
            public const string NamePrompt = "namePrompt";
        }

        private Attachment CreateAdpativeCardAttachment(MockData fakePayload)
        {
            AdaptiveCard card = new AdaptiveCard(new AdaptiveSchemaVersion(1, 0));
            card.Body.Add(new AdaptiveTextBlock()
            {
                Text = "ServiceNow Update Receipt Card",
                Size = AdaptiveTextSize.ExtraLarge
            });

            card.Body.Add(new AdaptiveFactSet()
            {
                Type = "FactSet",
                Facts = new List<AdaptiveFact>()
                {
                    new AdaptiveFact() { Title = "Id", Value = fakePayload.Id }
                }
            });
            card.Actions.Add(new AdaptiveSubmitAction()
            {
                Type = "Action.Submit",
                Title = "Click me for messageBack"
            });

            string jsonObj = card.ToJson();

            Attachment attachmentCard = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = JsonConvert.DeserializeObject(jsonObj)
            };

            return attachmentCard;
        }
    }
}
