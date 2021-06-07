﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Integration.AspNet.Core.Skills;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.BotFrameworkFunctionalTests.WaterfallHostBot.Dialogs;
using Microsoft.BotFrameworkFunctionalTests.WaterfallHostBot.Middleware;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallHostBot
{
    public class AdapterWithErrorHandler : BotFrameworkHttpAdapter
    {
        private readonly IConfiguration _configuration;
        private readonly ConversationState _conversationState;
        private readonly ILogger _logger;
        private readonly SkillHttpClient _skillClient;
        private readonly SkillsConfiguration _skillsConfig;

        public AdapterWithErrorHandler(IConfiguration configuration, ILogger<BotFrameworkHttpAdapter> logger, ConversationState conversationState, SkillHttpClient skillClient = null, SkillsConfiguration skillsConfig = null)
            : base(configuration, logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _conversationState = conversationState ?? throw new ArgumentNullException(nameof(conversationState));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _skillClient = skillClient;
            _skillsConfig = skillsConfig;

            OnTurnError = HandleTurnError;
            Use(new LoggerMiddleware(logger));
        }

        private async Task HandleTurnError(ITurnContext turnContext, Exception exception)
        {
            // Log any leaked exception from the application.
            _logger.LogError(exception, $"[OnTurnError] unhandled error : {exception.Message}");

            await SendErrorMessageAsync(turnContext, exception);
            await EndSkillConversationAsync(turnContext);
            await ClearConversationStateAsync(turnContext);
        }

        private async Task SendErrorMessageAsync(ITurnContext turnContext, Exception exception)
        {
            try
            {
                // Send a message to the user.
                var errorMessageText = "The bot encountered an error or bug.";
                var errorMessage = MessageFactory.Text(errorMessageText, errorMessageText, InputHints.IgnoringInput);
                errorMessage.Value = exception;
                await turnContext.SendActivityAsync(errorMessage);

                await turnContext.SendActivityAsync($"Exception: {exception.Message}");
                await turnContext.SendActivityAsync(exception.ToString());

                errorMessageText = "To continue to run this bot, please fix the bot source code.";
                errorMessage = MessageFactory.Text(errorMessageText, errorMessageText, InputHints.ExpectingInput);
                await turnContext.SendActivityAsync(errorMessage);

                // Send a trace activity, which will be displayed in the Bot Framework Emulator.
                await turnContext.TraceActivityAsync("OnTurnError Trace", exception.ToString(), "https://www.botframework.com/schemas/error", "TurnError");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception caught in SendErrorMessageAsync : {ex}");
            }
        }

        private async Task EndSkillConversationAsync(ITurnContext turnContext)
        {
            if (_skillClient == null || _skillsConfig == null)
            {
                return;
            }

            try
            {
                // Inform the active skill that the conversation is ended so that it has a chance to clean up.
                // Note: the root bot manages the ActiveSkillPropertyName, which has a value while the root bot
                // has an active conversation with a skill.
                var activeSkill = await _conversationState.CreateProperty<BotFrameworkSkill>(MainDialog.ActiveSkillPropertyName).GetAsync(turnContext, () => null);
                if (activeSkill != null)
                {
                    var botId = _configuration.GetSection(MicrosoftAppCredentials.MicrosoftAppIdKey)?.Value;

                    var endOfConversation = Activity.CreateEndOfConversationActivity();
                    endOfConversation.Code = "RootSkillError";
                    endOfConversation.ApplyConversationReference(turnContext.Activity.GetConversationReference(), true);

                    await _conversationState.SaveChangesAsync(turnContext, true);
                    await _skillClient.PostActivityAsync(botId, activeSkill, _skillsConfig.SkillHostEndpoint, (Activity)endOfConversation, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception caught on attempting to send EndOfConversation : {ex}");
            }
        }

        private async Task ClearConversationStateAsync(ITurnContext turnContext)
        {
            try
            {
                // Delete the conversationState for the current conversation to prevent the
                // bot from getting stuck in a error-loop caused by being in a bad state.
                // ConversationState should be thought of as similar to "cookie-state" for a Web page.
                await _conversationState.DeleteAsync(turnContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception caught on attempting to Delete ConversationState : {ex}");
            }
        }
    }
}
