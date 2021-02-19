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

        public MessagesTrigger(
            IEnumerable<IBotFrameworkHttpAdapter> adapters, 
            IEnumerable<AdapterSettings> adapterSettings, 
            IBot bot)
        {
            _bot = bot ?? throw new ArgumentNullException(nameof(bot));

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
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = "{adapter}")] HttpRequest req, string adapter)
        {
            if (string.IsNullOrEmpty(route))
            {
                throw new ArgumentNullException(nameof(route));
            }

            IBotFrameworkHttpAdapter adapter;

            if (_adapters.TryGetValue(route, out adapter))
            {
                // Delegate the processing of the HTTP POST to the appropriate adapter.
                // The adapter will invoke the bot.
                await _adapter.ProcessAsync(req, req.HttpContext.Response, _bot).ConfigureAwait(false);

                if (req.HttpContext.Response.IsSuccessStatusCode())
                {
                    return new OkResult();
                }
                
                return new ContentResult()
                {
                    StatusCode = req.HttpContext.Response.StatusCode,
                };
            }
            else
            {
                throw new KeyNotFoundException($"No adapter registered and enabled for route {route}.");
            }
        }
    }
}
