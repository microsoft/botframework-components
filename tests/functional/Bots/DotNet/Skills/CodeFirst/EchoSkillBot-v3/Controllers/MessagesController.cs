﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.SkillAuthentication;
using Microsoft.BotFrameworkFunctionalTests.EchoSkillBotv3.Authentication;

namespace Microsoft.BotFrameworkFunctionalTests.EchoSkillBotv3
{
    // Specify which type provides the authentication configuration to allow for validation for skills.
    [SkillBotAuthentication(AuthenticationConfigurationProviderType = typeof(CustomSkillAuthenticationConfiguration))]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.GetActivityType() == ActivityTypes.Message)
            {
                await Conversation.SendAsync(activity, () => new Dialogs.RootDialog());
            }
            else
            {
                await HandleSystemMessage(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private async Task<Activity> HandleSystemMessage(Activity message)
        {
            string messageType = message.GetActivityType();
            
            if (messageType == ActivityTypes.EndOfConversation)
            {
                Trace.TraceInformation($"EndOfConversation: {message}");

                // This Recipient null check is required for PVA manifest validation.
                // PVA will send an EOC activity with null Recipient.
                if (message.Recipient != null)
                {
                    // Clear the dialog stack if the root bot has ended the conversation.
                    using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, message))
                    {
                        var botData = scope.Resolve<IBotData>();
                        await botData.LoadAsync(default(CancellationToken));

                        var stack = scope.Resolve<IDialogStack>();
                        stack.Reset();

                        await botData.FlushAsync(default(CancellationToken));
                    }
                }
            }
            else if (messageType == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (messageType == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use Activity.MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
            }
            else if (messageType == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (messageType == ActivityTypes.Typing)
            {
                // Handle knowing that the user is typing
            }
            else if (messageType == ActivityTypes.Ping)
            {
            }

            return null;
        }
    }
}
