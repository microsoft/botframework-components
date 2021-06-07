// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Auth
{
    public class AuthDialog : ComponentDialog
    {
        private readonly string _connectionName;

        public AuthDialog(IConfiguration configuration)
            : base(nameof(AuthDialog))
        {
            _connectionName = configuration["ConnectionName"];

            // This confirmation dialog should be removed once https://github.com/microsoft/BotFramework-FunctionalTests/issues/299 is resolved (and this class should look like the class in the issue)
            AddDialog(new ConfirmPrompt(nameof(ConfirmPrompt)));
            AddDialog(new OAuthPrompt(
                nameof(OAuthPrompt),
                new OAuthPromptSettings
                {
                    ConnectionName = _connectionName,
                    Text = $"Please Sign In to connection: '{_connectionName}'",
                    Title = "Sign In",
                    Timeout = 300000 // User has 5 minutes to login (1000 * 60 * 5)
                }));

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), new WaterfallStep[] { PromptStepAsync, LoginStepAsync, DisplayTokenAsync }));

            // The initial child Dialog to run.
            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PromptStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            return await stepContext.BeginDialogAsync(nameof(OAuthPrompt), null, cancellationToken);
        }

        private async Task<DialogTurnResult> LoginStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the token from the previous step.
            var tokenResponse = (TokenResponse)stepContext.Result;
            if (tokenResponse != null)
            {
                stepContext.Values["Token"] = tokenResponse.Token;

                // Show the token
                var loggedInMessage = "You are now logged in.";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text(loggedInMessage, loggedInMessage, InputHints.IgnoringInput), cancellationToken);

                return await stepContext.PromptAsync(nameof(ConfirmPrompt), new PromptOptions { Prompt = MessageFactory.Text("Would you like to view your token?") }, cancellationToken);
            }

            var tryAgainMessage = "Login was not successful please try again.";
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(tryAgainMessage, tryAgainMessage, InputHints.IgnoringInput), cancellationToken);
            return await stepContext.ReplaceDialogAsync(InitialDialogId, cancellationToken: cancellationToken);
        }

        private async Task<DialogTurnResult> DisplayTokenAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var result = (bool)stepContext.Result;
            if (result)
            {
                var showTokenMessage = "Here is your token:";
                await stepContext.Context.SendActivityAsync(MessageFactory.Text($"{showTokenMessage} {stepContext.Values["Token"]}", showTokenMessage, InputHints.IgnoringInput), cancellationToken);
            }

            // Sign out
            var botAdapter = (BotFrameworkAdapter)stepContext.Context.Adapter;
            await botAdapter.SignOutUserAsync(stepContext.Context, _connectionName, null, cancellationToken);
            var signOutMessage = "I have signed you out.";
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(signOutMessage, signOutMessage, inputHint: InputHints.IgnoringInput), cancellationToken);

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }
    }
}
