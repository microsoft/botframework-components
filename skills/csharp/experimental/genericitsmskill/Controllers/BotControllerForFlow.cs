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
    // Controller to manager flow callback
    [Route("flow/messages/{encryptedChannelID}")]
    [ApiController]
    public class BotControllerForFlow : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly IBot _bot;
        private readonly string _appId;
        private readonly string _appPassword;
        private readonly IConfiguration _config;
        private readonly IDataProtectionProvider _dataProtectionProvider;

        public BotControllerForFlow(IServiceProvider serviceProvider, IBotFrameworkHttpAdapter adapter, IBot bot, IConfiguration config,
             IDataProtectionProvider dataProtectionProvider)
        {
            _config = serviceProvider.GetService<IConfiguration>();
            _adapter = serviceProvider.GetService<IBotFrameworkHttpAdapter>();
            _bot = bot;
            _appId = config["MicrosoftAppId"];
            _appPassword = config["MicrosoftAppPassword"];
            _config = config;
            _dataProtectionProvider = dataProtectionProvider;
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

            // Decrypt the channelID.
            var protector = _dataProtectionProvider.CreateProtector("test");
            var channelID = protector.Unprotect(encryptedChannelID);

            // Deserialize the body to FlowHttpRequestData format, and deserialize comments of the github issue.
            var dataFromRequestString = JsonConvert.DeserializeObject(bodyStr).ToString();
            var serviceNowNotification = JsonConvert.DeserializeObject<ServiceDeskNotification>(dataFromRequestString);
            serviceNowNotification.ChannelId = channelID;

            // Create New Activity
            var activity = new Activity
            {
                Type = ActivityTypes.Event,
                ChannelId = "ServiceDeskNotification",
                Conversation = new ConversationAccount(id: $"{Guid.NewGuid()}"),
                From = new ChannelAccount(id: $"Notification.ServiceDesk", name: $"Notification.GenericITSMSkill"),
                Recipient = new ChannelAccount(id: $"Notification.ServiceDesk", name: $"Notification.GenericITSMSkill"),
                Name = "Proactive",
                Value = JsonConvert.SerializeObject(serviceNowNotification)
            };

            // Send Activity to the bot to process as a Proactive event
            await _bot.OnTurnAsync(new TurnContext((BotAdapter)_adapter, activity), CancellationToken.None);
        }
    }
}
