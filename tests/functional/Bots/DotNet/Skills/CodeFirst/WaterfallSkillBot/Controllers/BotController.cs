// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Extensions.Logging;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Controllers
{
    // This ASP Controller is created to handle a request. Dependency Injection will provide the Adapter and IBot
    // implementation at runtime. Multiple different IBot implementations running at different endpoints can be
    // achieved by specifying a more specific type for the bot constructor argument.
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly IBotFrameworkHttpAdapter _adapter;
        private readonly IBot _bot;
        private readonly ILogger _logger;

        public BotController(BotFrameworkHttpAdapter adapter, IBot bot, ILogger<BotController> logger)
        {
            _adapter = adapter;
            _bot = bot;
            _logger = logger;
        }

        [Route("api/messages")]
        [HttpGet]
        [HttpPost]
        public async Task PostAsync()
        {
            try
            {
                // Delegate the processing of the HTTP POST to the adapter.
                // The adapter will invoke the bot.
                await _adapter.ProcessAsync(Request, Response, _bot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request");
                throw;
            }
        }
    }
}
