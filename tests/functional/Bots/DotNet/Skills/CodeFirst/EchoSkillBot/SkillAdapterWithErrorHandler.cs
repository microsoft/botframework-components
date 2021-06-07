// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Microsoft.BotFrameworkFunctionalTests.EchoSkillBot
{
    public class SkillAdapterWithErrorHandler : BotFrameworkHttpAdapter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SkillAdapterWithErrorHandler"/> class to handle errors.
        /// </summary>
        /// <param name="configuration">The configuration properties.</param>
        /// <param name="credentialProvider">An implementation of the bots credentials.</param>
        /// <param name="authConfig">The configuration setting for the authentication.</param>
        /// <param name="logger">An instance of a logger.</param>
        public SkillAdapterWithErrorHandler(IConfiguration configuration, ICredentialProvider credentialProvider, AuthenticationConfiguration authConfig, ILogger<BotFrameworkHttpAdapter> logger)
            : base(configuration, credentialProvider, authConfig, logger: logger)
        {
            OnTurnError = async (turnContext, exception) =>
            {
                try
                {
                    // Log any leaked exception from the application.
                    logger.LogError(exception, $"[OnTurnError] unhandled error : {exception.Message}");

                    // Send a message to the user
                    var errorMessageText = "The skill encountered an error or bug.";
                    var errorMessage = MessageFactory.Text(errorMessageText + Environment.NewLine + exception, errorMessageText, InputHints.IgnoringInput);
                    errorMessage.Value = exception;
                    await turnContext.SendActivityAsync(errorMessage);

                    errorMessageText = "To continue to run this bot, please fix the bot source code.";
                    errorMessage = MessageFactory.Text(errorMessageText, errorMessageText, InputHints.ExpectingInput);
                    await turnContext.SendActivityAsync(errorMessage);

                    // Send a trace activity, which will be displayed in the Bot Framework Emulator
                    // Note: we return the entire exception in the value property to help the developer, this should not be done in prod.
                    await turnContext.TraceActivityAsync("OnTurnError Trace", exception.ToString(), "https://www.botframework.com/schemas/error", "TurnError");

                    // Send and EndOfConversation activity to the skill caller with the error to end the conversation
                    // and let the caller decide what to do.
                    var endOfConversation = Activity.CreateEndOfConversationActivity();
                    endOfConversation.Code = "SkillError";
                    endOfConversation.Text = exception.Message;
                    await turnContext.SendActivityAsync(endOfConversation);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Exception caught in SkillAdapterWithErrorHandler : {ex}");
                }
            };
        }
    }
}
