﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Sso
{
    public class SsoSkillDialog : ComponentDialog
    {
        private readonly string _connectionName;

        public SsoSkillDialog(IConfiguration configuration)
            : base(nameof(SsoSkillDialog))
        {
            _connectionName = configuration.GetSection("SsoConnectionName")?.Value;
            AddDialog(new SsoSkillSignInDialog(_connectionName));
            AddDialog(new ChoicePrompt("ActionStepPrompt"));

            var waterfallSteps = new WaterfallStep[]
            {
                PromptActionStepAsync,
                HandleActionStepAsync,
                PromptFinalStepAsync
            };

            AddDialog(new WaterfallDialog(nameof(WaterfallDialog), waterfallSteps));

            InitialDialogId = nameof(WaterfallDialog);
        }

        private async Task<DialogTurnResult> PromptActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            const string messageText = "What SSO action would you like to perform on the skill?";
            const string repromptMessageText = "That was not a valid choice, please select a valid choice.";
            var options = new PromptOptions
            {
                Prompt = MessageFactory.Text(messageText, messageText, InputHints.ExpectingInput),
                RetryPrompt = MessageFactory.Text(repromptMessageText, repromptMessageText, InputHints.ExpectingInput),
                Choices = await GetPromptChoicesAsync(stepContext, cancellationToken)
            };

            // Prompt the user to select a skill.
            return await stepContext.PromptAsync("ActionStepPrompt", options, cancellationToken);
        }

        private async Task<List<Choice>> GetPromptChoicesAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var promptChoices = new List<Choice>();
            var adapter = (IUserTokenProvider)stepContext.Context.Adapter;
            var token = await adapter.GetUserTokenAsync(stepContext.Context, _connectionName, null, cancellationToken);

            if (token == null)
            {
                promptChoices.Add(new Choice("Login"));
            }
            else
            {
                promptChoices.Add(new Choice("Logout"));
                promptChoices.Add(new Choice("Show token"));
            }

            promptChoices.Add(new Choice("End"));

            return promptChoices;
        }

        private async Task<DialogTurnResult> HandleActionStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            var action = ((FoundChoice)stepContext.Result).Value.ToLowerInvariant();

            switch (action)
            {
                case "login":
                    return await stepContext.BeginDialogAsync(nameof(SsoSkillSignInDialog), null, cancellationToken);

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

                case "end":
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
