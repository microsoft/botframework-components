// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Middleware
{
    /// <summary>
    /// A middleware that ensures conversation state is saved when an OAuthCard is returned by the skill.
    /// </summary>
    /// <remarks>
    /// In SSO, the host will send an Invoke with the token if SSO is enabled.
    /// This middleware saves the state of the bot before sending out the SSO card to ensure the dialog state
    /// is persisted and in the right state if an InvokeActivity comes back from the Host with the token.
    /// </remarks>
    public class SsoSaveStateMiddleware : IMiddleware
    {
        private readonly ConversationState _conversationState;

        public SsoSaveStateMiddleware(ConversationState conversationState)
        {
            _conversationState = conversationState;
        }

        public async Task OnTurnAsync(ITurnContext turnContext, NextDelegate next, CancellationToken cancellationToken = new CancellationToken())
        {
            // Register outgoing handler.
            turnContext.OnSendActivities(OutgoingHandler);

            // Continue processing messages.
            await next(cancellationToken);
        }

        private async Task<ResourceResponse[]> OutgoingHandler(ITurnContext turnContext, List<Activity> activities, Func<Task<ResourceResponse[]>> next)
        {
            foreach (var activity in activities)
            {
                // Check if any of the outgoing activities has an OAuthCard.
                if (activity.Attachments != null && activity.Attachments.Any(attachment => attachment.ContentType == OAuthCard.ContentType))
                {
                    // Save any state changes so the dialog stack is ready for SSO exchanges.
                    await _conversationState.SaveChangesAsync(turnContext, false, CancellationToken.None);
                }
            }

            return await next();
        }
    }
}
