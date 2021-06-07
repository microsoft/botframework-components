// Copyright (c) Microsoft Corporation. All rights reserved.
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
using Microsoft.BotFrameworkFunctionalTests.SimpleHostBot.Bots;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.BotFrameworkFunctionalTests.SimpleHostBot
{
    public class AdapterWithErrorHandler : BotFrameworkHttpAdapter
    {
        private readonly ConversationState _conversationState;
        private readonly IConfiguration _configuration;
        private readonly ILogger _logger;
        private readonly SkillHttpClient _skillClient;
        private readonly SkillsConfiguration _skillsConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdapterWithErrorHandler"/> class to handle errors.
        /// </summary>
        /// <param name="configuration">The configuration properties.</param>
        /// <param name="logger">An instance of a logger.</param>
        /// <param name="conversationState">A state management object for the conversation.</param>
        /// <param name="skillClient">The HTTP client for the skills.</param>
        /// <param name="skillsConfig">The skills configuration.</param>
        public AdapterWithErrorHandler(IConfiguration configuration, ILogger<BotFrameworkHttpAdapter> logger, ConversationState conversationState = null, SkillHttpClient skillClient = null, SkillsConfiguration skillsConfig = null)
            : base(configuration, logger)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _conversationState = conversationState;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _skillClient = skillClient;
            _skillsConfig = skillsConfig;

            OnTurnError = HandleTurnErrorAsync;
        }

        /// <summary>
        /// Handles the error by sending a message to the user and ending the conversation with the skill.
        /// </summary>
        /// <param name="turnContext">Context for the current turn of conversation.</param>
        /// <param name="exception">The handled exception.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        private async Task HandleTurnErrorAsync(ITurnContext turnContext, Exception exception)
        {
            // Log any leaked exception from the application.
            _logger.LogError(exception, $"[OnTurnError] unhandled error : {exception.Message}");

            await SendErrorMessageAsync(turnContext, exception, default);
            await EndSkillConversationAsync(turnContext, default);
            await ClearConversationStateAsync(turnContext, default);
        }

        /// <summary>
        /// Sends an error message to the user and a trace activity to be displayed in the Bot Framework Emulator.
        /// </summary>
        /// <param name="turnContext">Context for the current turn of conversation.</param>
        /// <param name="exception">The exception to be sent in the message.</param>
        /// <param name="cancellationToken">CancellationToken propagates notifications that operations should be cancelled.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        private async Task SendErrorMessageAsync(ITurnContext turnContext, Exception exception, CancellationToken cancellationToken)
        {
            try
            {
                // Send a message to the user
                var errorMessageText = "The bot encountered an error or bug.";
                var errorMessage = MessageFactory.Text(errorMessageText, errorMessageText, InputHints.IgnoringInput);
                errorMessage.Value = exception;
                await turnContext.SendActivityAsync(errorMessage, cancellationToken);

                await turnContext.SendActivityAsync($"Exception: {exception.Message}");
                await turnContext.SendActivityAsync(exception.ToString());

                errorMessageText = "To continue to run this bot, please fix the bot source code.";
                errorMessage = MessageFactory.Text(errorMessageText, errorMessageText, InputHints.ExpectingInput);
                await turnContext.SendActivityAsync(errorMessage, cancellationToken);

                // Send a trace activity, which will be displayed in the Bot Framework Emulator
                await turnContext.TraceActivityAsync("OnTurnError Trace", exception.ToString(), "https://www.botframework.com/schemas/error", "TurnError", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception caught in SendErrorMessageAsync : {ex}");
            }
        }

        /// <summary>
        /// Informs to the active skill that the conversation is ended so that it has a chance to clean up.
        /// </summary>
        /// <param name="turnContext">Context for the current turn of conversation.</param>
        /// <param name="cancellationToken">CancellationToken propagates notifications that operations should be cancelled.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        private async Task EndSkillConversationAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            if (_conversationState == null || _skillClient == null || _skillsConfig == null)
            {
                return;
            }

            try
            {
                // Note: ActiveSkillPropertyName is set by the HostBot while messages are being
                // forwarded to a Skill.
                var activeSkill = await _conversationState.CreateProperty<BotFrameworkSkill>(HostBot.ActiveSkillPropertyName).GetAsync(turnContext, () => null, cancellationToken);
                if (activeSkill != null)
                {
                    var botId = _configuration.GetSection(MicrosoftAppCredentials.MicrosoftAppIdKey)?.Value;

                    var endOfConversation = Activity.CreateEndOfConversationActivity();
                    endOfConversation.Code = "HostSkillError";
                    endOfConversation.ApplyConversationReference(turnContext.Activity.GetConversationReference(), true);

                    await _conversationState.SaveChangesAsync(turnContext, true, cancellationToken);
                    await _skillClient.PostActivityAsync(botId, activeSkill, _skillsConfig.SkillHostEndpoint, (Activity)endOfConversation, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Exception caught on attempting to send EndOfConversation : {ex}");
            }
        }

        /// <summary>
        /// Deletes the conversationState for the current conversation to prevent the bot from getting stuck in an error-loop caused by being in a bad state.
        /// ConversationState should be thought of as similar to "cookie-state" in a Web pages.
        /// </summary>
        /// <param name="turnContext">Context for the current turn of conversation.</param>
        /// <param name="cancellationToken">CancellationToken propagates notifications that operations should be cancelled.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        private async Task ClearConversationStateAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            if (_conversationState != null)
            {
                try
                {
                    await _conversationState.DeleteAsync(turnContext, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Exception caught on attempting to Delete ConversationState : {ex}");
                }
            }
        }
    }
}
