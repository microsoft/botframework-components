// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Bots
{
    public class SkillBot<T> : ActivityHandler
        where T : Dialog
    {
        private readonly ConversationState _conversationState;
        private readonly Dialog _mainDialog;
        private readonly Uri _serverUrl;

        public SkillBot(ConversationState conversationState, T mainDialog, IHttpContextAccessor httpContextAccessor)
        {
            _conversationState = conversationState;
            _mainDialog = mainDialog;
            _serverUrl = new Uri($"{httpContextAccessor.HttpContext.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host.Value}");
        }

        public override async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default)
        {
            if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                // Let the base class handle the activity (this will trigger OnMembersAddedAsync).
                await base.OnTurnAsync(turnContext, cancellationToken);
            }
            else
            {
                // Run the Dialog with the Activity.
                await _mainDialog.RunAsync(turnContext, _conversationState.CreateProperty<DialogState>("DialogState"), cancellationToken);
            }

            // Save any state changes that might have occurred during the turn.
            await _conversationState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    var activity = MessageFactory.Text("Welcome to the waterfall skill bot. \n\nThis is a skill, you will need to call it from another bot to use it.");
                    activity.Speak = "Welcome to the waterfall skill bot. This is a skill, you will need to call it from another bot to use it.";
                    await turnContext.SendActivityAsync(activity, cancellationToken);

                    await turnContext.SendActivityAsync($"You can check the skill manifest to see what it supports here: {_serverUrl}manifests/waterfallskillbot-manifest-1.0.json", cancellationToken: cancellationToken);
                }
            }
        }
    }
}
