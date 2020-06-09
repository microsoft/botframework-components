using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using AdaptiveCards;
using GenericITSMSkill.Extensions;
using GenericITSMSkill.Models;
using GenericITSMSkill.Models.ServiceDesk;
using GenericITSMSkill.UpdateActivity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Schema.Teams;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Attachment = Microsoft.Bot.Schema.Attachment;

namespace GenericITSMSkill.Controllers
{
    // This ASP Controller is created to handle a request. Dependency Injection will provide the Adapter and IBot
    // implementation at runtime. Multiple different IBot implementations running at different endpoints can be
    // achieved by specifying a more specific type for the bot constructor argument.
    [Route("flow/messages/{encryptedChannelID}")]
    [ApiController]
    public class BotControllerForFlow : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter Adapter;
        private readonly IBot Bot;
        private readonly string _appId;
        private readonly string _appPassword;
        private ServiceDeskNotification serviceNowNotification;
        private IDocumentClient _documentClient;
        private readonly IConfiguration _config;
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly ConversationState _conversationState;
        private readonly IStatePropertyAccessor<ActivityReferenceMap> _activityReferenceMapAccessor;
        private readonly IStatePropertyAccessor<TicketIdCorrelationMap> _ticketIdCorrelationMapAccessor;
        private string _encryptionKey;
        private string channelID;

        public BotControllerForFlow(IServiceProvider serviceProvider, IBotFrameworkHttpAdapter adapter, IBot bot, IConfiguration config,
            IDocumentClient documentClient, IDataProtectionProvider dataProtectionProvider, String encryptionKey)
        {
            _config = serviceProvider.GetService<IConfiguration>();
            _activityReferenceMapAccessor = _conversationState.CreateProperty<ActivityReferenceMap>(nameof(ActivityReferenceMap));
            _ticketIdCorrelationMapAccessor = _conversationState.CreateProperty<TicketIdCorrelationMap>(nameof(TicketIdCorrelationMap));

            Adapter = adapter;
            Bot = bot;
            _appId = config["MicrosoftAppId"];
            _appPassword = config["MicrosoftAppPassword"];
            _documentClient = documentClient;
            _config = config;
            _dataProtectionProvider = dataProtectionProvider;
            _encryptionKey = encryptionKey;
        }

        [HttpPost]
        public async Task PostAsync(string encryptedChannelID)
        {
            // 0) Get request body data.
            string bodyStr;
            using (StreamReader reader
                  = new StreamReader(this.Request.Body, Encoding.UTF8, true, 1024, true))
            {
                bodyStr = await reader.ReadToEndAsync();
            }

            // 0) Decrypt the channelID.
            var protector = _dataProtectionProvider.CreateProtector("test");
            channelID = protector.Unprotect(encryptedChannelID);

            // 1) Deserialize the body to FlowHttpRequestData format, and deserialize comments of the github issue.
            var dataFromRequestString = JsonConvert.DeserializeObject(bodyStr).ToString();
            serviceNowNotification = JsonConvert.DeserializeObject<ServiceDeskNotification>(dataFromRequestString);
            serviceNowNotification.ChannelId = channelID;

            var activity = new Activity
            {
                Type = ActivityTypes.Event,
                ChannelId = "ServicenowNotification",
                Conversation = new ConversationAccount(id: $"{Guid.NewGuid()}"),
                From = new ChannelAccount(id: $"Notification.ServicenowWebhook", name: $"Notification.ITSMSkill"),
                Recipient = new ChannelAccount(id: $"Notification.ServicenowWebhook", name: $"Notification.ITSMSkill"),
                Name = "Proactive",
                Value = JsonConvert.SerializeObject(serviceNowNotification)
            };

            await Bot.OnTurnAsync(new TurnContext((BotAdapter)Adapter, activity), CancellationToken.None);
        }

        private async Task ServiceNowNotificationBotCallbackForSend(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            // Create a card by using the data from the request body.
            var card = serviceNowNotification.ToAdaptiveCard();

            Attachment attachmentCard = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = JsonConvert.DeserializeObject(card.ToJson())
            };

            TicketIdCorrelationMap ticketReferenceMap = await _ticketIdCorrelationMapAccessor.GetAsync(
               turnContext,
               () => new TicketIdCorrelationMap(),
               cancellationToken)
           .ConfigureAwait(false);

            ticketReferenceMap.TryGetValue(channelID, out var ticketCorrelationId);

            ActivityReferenceMap activityReferenceMap = await _activityReferenceMapAccessor.GetAsync(
               turnContext,
               () => new ActivityReferenceMap(),
               cancellationToken)
           .ConfigureAwait(false);

            activityReferenceMap.TryGetValue(ticketCorrelationId.ThreadId, out var activityReference);

            var cardReplyActivity = MessageFactory.Attachment(attachmentCard);
            cardReplyActivity.Id = activityReference.ActivityId;
            await turnContext.SendActivityAsync(cardReplyActivity);
        }
    }
}
