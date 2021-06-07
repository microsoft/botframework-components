// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Skills;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallHostBot.Controllers
{
    /// <summary>
    /// A controller that handles skill replies to the bot.
    /// This example uses the <see cref="SkillHandler"/> that is registered as a <see cref="ChannelServiceHandler"/> in startup.cs.
    /// </summary>
    [ApiController]
    [Route("api/skills")]
    public class SkillController : ChannelServiceController
    {
        private readonly ILogger _logger;

        public SkillController(ChannelServiceHandler handler, ILogger<SkillController> logger)
            : base(handler)
        {
            _logger = logger;
        }

        public override Task<IActionResult> ReplyToActivityAsync(string conversationId, string activityId, Activity activity)
        {
            try
            {
                return base.ReplyToActivityAsync(conversationId, activityId, activity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request");
                throw;
            }
        }

        public override Task<IActionResult> SendToConversationAsync(string conversationId, Activity activity)
        {
            try
            {
                return base.SendToConversationAsync(conversationId, activity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request");
                throw;
            }
        }

        public override Task<IActionResult> UpdateActivityAsync(string conversationId, string activityId, Activity activity)
        {
            try
            {
                return base.UpdateActivityAsync(conversationId, activityId, activity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request");
                throw;
            }
        }

        public override Task DeleteActivityAsync(string conversationId, string activityId)
        {
            try
            {
                return base.DeleteActivityAsync(conversationId, activityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request");
                throw;
            }
        }
    }
}
