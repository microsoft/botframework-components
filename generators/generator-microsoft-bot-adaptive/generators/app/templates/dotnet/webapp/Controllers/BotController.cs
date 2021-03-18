﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Integration.Runtime.Settings;

namespace <%= botName %>.Controllers
{
    // This ASP Controller is created to handle a request. Dependency Injection will provide the Adapter and IBot
    // implementation at runtime. Multiple different IBot implementations running at different endpoints can be
    // achieved by specifying a more specific type for the bot constructor argument.
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly Dictionary<string, IBotFrameworkHttpAdapter> _adapters = new Dictionary<string, IBotFrameworkHttpAdapter>();
        private readonly IBot _bot;
        private readonly ILogger<BotController> _logger;

        public BotController(
            IEnumerable<IBotFrameworkHttpAdapter> adapters,
            IEnumerable<AdapterSettings> adapterSettings,
            IBot bot,
            ILogger<BotController> logger)
        {
            _bot = bot ?? throw new ArgumentNullException(nameof(bot));
            _logger = logger;

            foreach (var adapter in adapters ?? throw new ArgumentNullException(nameof(adapters)))
            {
                var settings = adapterSettings.FirstOrDefault(s => s.Enabled && s.Name == adapter.GetType().FullName);

                if (settings != null)
                {
                    _adapters.Add(settings.Route, adapter);
                }
            }
        }

        [HttpPost]
        [HttpGet]
        [Route("api/{route}")]
        public async Task PostAsync(string route)
        {
            if (string.IsNullOrEmpty(route))
            {
                _logger.LogError($"PostAsync: No route provided.");
                throw new ArgumentNullException(nameof(route));
            }

            if (_adapters.TryGetValue(route, out IBotFrameworkHttpAdapter adapter))
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogInformation($"PostAsync: routed '{route}' to {adapter.GetType().Name}");
                }

                // Delegate the processing of the HTTP POST to the appropriate adapter.
                // The adapter will invoke the bot.
                await adapter.ProcessAsync(Request, Response, _bot).ConfigureAwait(false);
            }
            else
            {
                _logger.LogError($"PostAsync: No adapter registered and enabled for route {route}.");
                throw new KeyNotFoundException($"No adapter registered and enabled for route {route}.");
            }
        }
    }
}
