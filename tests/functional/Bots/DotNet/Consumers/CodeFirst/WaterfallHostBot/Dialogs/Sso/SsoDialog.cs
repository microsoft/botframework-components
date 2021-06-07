// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallHostBot.Dialogs.Sso
{
    /// <summary>
    /// Helps prepare the host for SSO operations and provides helpers to check the status and invoke the skill.
    /// </summary>
    public class SsoDialog : ComponentDialog
    {
        private readonly string _connectionName;
        private readonly string _skillDialogId;

        public SsoDialog(string dialogId, Dialog ssoSkillDialog, string connectionName)
            : base(dialogId)
        {
            _connectionName = connectionName;
            _skillDialogId = ssoSkillDialog.Id;

            AddDialog(new ChoicePrompt("ActionStepPrompt"));
            AddDialog(new SsoSignInDialog(_connectionName));
            AddDialog(ssoSkillDialog);

            var waterfallSteps = new WaterfallStep[]
            {
                PromptActionStepAsync,
                HandleActionStepAsync,
                PromptFinalStepAsync,
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PromptActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var messageText = "What SSO action do you want to perform?";
            var repromptMessageText = "That was not a valid choice, please select a valid choice.";
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
                Choices = await GetPromptChoicesAsync(stepContext, cancellationToken)
            };

            // Prompt the user to select a skill.
            return await stepContext.PromptAsync("ActionStepPrompt", options, cancellationToken);
        }

        // Create the prompt choices based on the current sign in status
        private async Task<List<Choice>> GetPromptChoicesAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var promptChoices = new List<Choice>();
            var adapter = (IUserTokenProvider)stepContext.Context.Adapter;
            var token = await adapter.GetUserTokenAsync(stepContext.Context, _connectionName, null, cancellationToken);

            if (token == null)
            {
                promptChoices.Add(new Choice("Login"));

                // Token exchange will fail when the host is not logged on and the skill should 
                // show a regular OAuthPrompt
                promptChoices.Add(new Choice("Call Skill (without SSO)"));
            }
            else
            {
                promptChoices.Add(new Choice("Logout"));
                promptChoices.Add(new Choice("Show token"));
                promptChoices.Add(new Choice("Call Skill (with SSO)"));
            }

            promptChoices.Add(new Choice("Back"));

            return promptChoices;
        }

        private async Task<DialogTurnResult> HandleActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var action = ((FoundChoice)stepContext.Result).Value.ToLowerInvariant();

            switch (action)
            {
                case "login":
                    return await stepContext.BeginDialogAsync(nameof(SsoSignInDialog), null, cancellationToken);

                case "logout":
                    var adapter = (IUserTokenProvider)stepContext.Context.Adapter;
                    await adapter.SignOutUserAsync(stepContext.Context, _connectionName, cancellationToken: cancellationToken);
                    await stepContext.Context.SendActivityAsync("You have been signed out.", cancellationToken: cancellationToken);
                    return await stepContext.NextAsync(cancellationToken: cancellationToken);

                case "show token":
                    var tokenProvider = (IUserTokenProvider)stepContext.Context.Adapter;
                    var token = await tokenProvider.GetUserTokenAsync(stepContext.Context, _connectionName, null, cancellationToken);
                    if (token == null)
                    {
                        await stepContext.Context.SendActivityAsync("User has no cached token.", cancellationToken: cancellationToken);
                    }
                    else
                    {
                        await stepContext.Context.SendActivityAsync($"Here is your current SSO token: {token.Token}", cancellationToken: cancellationToken);
                    }

                    return await stepContext.NextAsync(cancellationToken: cancellationToken);

                case "call skill (with sso)":
                case "call skill (without sso)":
                    var beginSkillActivity = new Activity
                    {
                        Type = ActivityTypes.Event,
                        Name = "Sso"
                    };

                    return await stepContext.BeginDialogAsync(_skillDialogId, new BeginSkillDialogOptions { Activity = beginSkillActivity }, cancellationToken);

                case "back":
                    return new DialogTurnResult(DialogTurnStatus.Complete);

                default:
                    // This should never be hit since the previous prompt validates the choice
                    throw new InvalidOperationException($"Unrecognized action: {action}");
            }
        }

        private async Task<DialogTurnResult> PromptFinalStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Restart the dialog (we will exit when the user says end)
            return await stepContext.ReplaceDialogAsync(InitialDialogId, null, cancellationToken);
        }
    }
}
