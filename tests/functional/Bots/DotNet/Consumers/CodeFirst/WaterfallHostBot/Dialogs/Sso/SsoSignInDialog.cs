﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallHostBot.Dialogs.Sso
{
    public class SsoSignInDialog : ComponentDialog
    {
        public SsoSignInDialog(string connectionName)
            : base(nameof(SsoSignInDialog))
        {
            AddDialog(new OAuthPrompt(
                nameof(OAuthPrompt), 
                new OAuthPromptSettings
            {
                ConnectionName = connectionName,
                Text = $"Sign in to the host bot using AAD for SSO and connection {connectionName}",
                Title = "Sign In",
                Timeout = 60000
            }));
            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[] { SignInStepAsync, DisplayTokenAsync }));
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> SignInStepAsync(WaterfallStepContext context, CancellationToken cancellationToken)
        {
            return await context.BeginDialogAsync(nameof(OAuthPrompt), null, cancellationToken);
        }

        private async Task<DialogTurnResult> DisplayTokenAsync(WaterfallStepContext context, CancellationToken cancellationToken)
        {
            if (!(context.Result is TokenResponse result))
            {
                await context.Context.SendActivityAsync("No token was provided.", cancellationToken: cancellationToken);
            }
            else
            {
                await context.Context.SendActivityAsync($"Here is your token: {result.Token}", cancellationToken: cancellationToken);
            }

            return await context.EndDialogAsync(null, cancellationToken);
        }
    }
}
