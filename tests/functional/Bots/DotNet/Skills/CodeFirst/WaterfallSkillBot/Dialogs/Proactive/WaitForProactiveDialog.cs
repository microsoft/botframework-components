// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Proactive
{
    public class WaitForProactiveDialog : Dialog
    {
        // Message to send to users when the bot receives a Conversation Update event
        private const string NotifyMessage = "Navigate to {0}api/notify?user={1} to proactively message the user.";
        private readonly ConcurrentDictionary<string, ContinuationParameters> _continuationParametersStore;

        private readonly Uri _serverUrl;

        public WaitForProactiveDialog(IHttpContextAccessor httpContextAccessor, ConcurrentDictionary<string, ContinuationParameters> continuationParametersStore)
        {
            _continuationParametersStore = continuationParametersStore;
            _serverUrl = new Uri($"{httpContextAccessor.HttpContext.Request.Scheme}://{httpContextAccessor.HttpContext.Request.Host.Value}");
        }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            // Store a reference to the conversation.
            AddOrUpdateContinuationParameters(dc.Context);

            // Render message with continuation link.
            await dc.Context.SendActivityAsync(MessageFactory.Text(string.Format(NotifyMessage, _serverUrl, dc.Context.Activity.From.Id)), cancellationToken);
            return EndOfTurn;
        }

        public override async Task<DialogTurnResult> ContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default)
        {
            if (dc.Context.Activity.Type == ActivityTypes.Event && dc.Context.Activity.Name == ActivityEventNames.ContinueConversation)
            {
                // We continued the conversation, forget the proactive reference.
                _continuationParametersStore.TryRemove(dc.Context.Activity.From.Id, out _);

                // The continue conversation activity comes from the ProactiveController when the notification is received
                await dc.Context.SendActivityAsync("We received a proactive message, ending the dialog", cancellationToken: cancellationToken);

                // End the dialog so the host gets an EoC
                return new DialogTurnResult(DialogTurnStatus.Complete);
            }

            // Keep waiting for a call to the ProactiveController.
            await dc.Context.SendActivityAsync($"We are waiting for a proactive message. {string.Format(NotifyMessage, _serverUrl, dc.Context.Activity.From.Id)}", cancellationToken: cancellationToken);

            return EndOfTurn;
        }

        /// <summary>
        /// Helper to extract and store parameters we need to continue a conversation from a proactive message.
        /// </summary>
        /// <param name="turnContext">A turnContext instance with the parameters we need.</param>
        private void AddOrUpdateContinuationParameters(ITurnContext turnContext)
        {
            var continuationParameters = new ContinuationParameters
            {
                ClaimsIdentity = turnContext.TurnState.Get<IIdentity>(BotAdapter.BotIdentityKey),
                ConversationReference = turnContext.Activity.GetConversationReference(),
                OAuthScope = turnContext.TurnState.Get<string>(BotAdapter.OAuthScopeKey)
            };

            _continuationParametersStore.AddOrUpdate(turnContext.Activity.From.Id, continuationParameters, (_, __) => continuationParameters);
        }
    }
}
