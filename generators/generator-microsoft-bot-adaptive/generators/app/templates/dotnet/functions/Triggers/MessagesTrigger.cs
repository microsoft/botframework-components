using System;
using System.Threading.Tasks;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Integration.Runtime.Settings;

namespace <%= botName %>.Triggers
{
    /// <summary>
    /// Functions trigger for Bot Framework messages.
    /// </summary>
    public class MessagesTrigger
    {
        private readonly Dictionary<string, IBotFrameworkHttpAdapter> _adapters = new Dictionary<string, IBotFrameworkHttpAdapter>();
        private readonly IBot _bot;
        private readonly ILogger<BotController> _logger;

        public MessagesTrigger(
            IEnumerable<IBotFrameworkHttpAdapter> adapters,
            IEnumerable<AdapterSettings> adapterSettings,
            IBot bot,
            ILogger<MessagesTrigger> logger)
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

        /// <summary>
        /// Bot Framework messages trigger handling.
        /// </summary>
        /// <param name="req">
        /// The <see cref="HttpRequest"/>.
        /// </param>
        /// <returns>
        /// The <see cref="IActionResult"/>.
        /// </returns>
        [FunctionName("messages")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "{adapterRoute}")] HttpRequest req, string adapterRoute)
        {
            if (string.IsNullOrEmpty(adapterRoute))
            {
                _logger.LogError($"RunAsync: No route provided.");
                throw new ArgumentNullException(nameof(adapterRoute));
            }

            if (_adapters.TryGetValue(adapterRoute, out IBotFrameworkHttpAdapter adapter))
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogInformation($"RunAsync: routed '{adapterRoute}' to {adapter.GetType().Name}");
                }

                // Delegate the processing of the HTTP POST to the appropriate adapter.
                // The adapter will invoke the bot.
                await adapter.ProcessAsync(req, req.HttpContext.Response, _bot).ConfigureAwait(false);
                if (req.HttpContext.Response.IsSuccessStatusCode())
                {
                    return new OkResult();
                }
                return new ContentResult()
                {
                    StatusCode = req.HttpContext.Response.StatusCode,
                };
            }

            _logger.LogError($"RunAsync: No adapter registered and enabled for route {adapterRoute}.");
            throw new KeyNotFoundException($"No adapter registered and enabled for route {adapterRoute}.");
        }
    }
}
