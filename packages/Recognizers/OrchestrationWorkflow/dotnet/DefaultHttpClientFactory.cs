using System;
using System.Net.Http;
using Microsoft.Rest;

namespace Microsoft.Bot.Components.Recognizers.CLURecognizer
{
    /// <summary>
    /// HttpClientFactory that always returns the same HttpClient instance for CLU calls.
    /// </summary>
    internal class DefaultHttpClientFactory : IHttpClientFactory
    {
        private static readonly HttpClient _httpClient = new HttpClient(CreateHttpHandlerPipeline(CreateRootHandler(), new CluDelegatingHandler()), false)
        {
            Timeout = TimeSpan.FromMilliseconds(CluConstants.HttpClientOptions.Timeout),
        };
        
        /// <summary>
        /// Returns the same default HttpClient instance.
        /// </summary>
        /// <param name="name">Name is not used in this context. This parameter is here, because it is dictated by the <see cref="IHttpClientFactory"/> interface method declaration.</param>
        /// <returns>The same HttpClient instance.</returns>
        public HttpClient CreateClient(string name)
        {
            return _httpClient;
        }

        private static HttpClientHandler CreateRootHandler() => new HttpClientHandler();

        private static DelegatingHandler CreateHttpHandlerPipeline(HttpClientHandler httpClientHandler, params DelegatingHandler[] handlers)
        {
            // Now, the RetryAfterDelegatingHandler should be the absolute outermost handler
            // because it's extremely lightweight and non-interfering
            DelegatingHandler currentHandler =
#pragma warning disable CA2000 // Dispose objects before losing scope (suppressing this warning, for now! we will address this once we implement HttpClientFactory in a future release)
                new RetryDelegatingHandler(
                    new RetryAfterDelegatingHandler 
                    { 
                        InnerHandler = httpClientHandler 
                    });
#pragma warning restore CA2000 // Dispose objects before losing scope

            if (handlers != null)
            {
                for (var i = handlers.Length - 1; i >= 0; --i)
                {
                    var handler = handlers[i];

                    // Non-delegating handlers are ignored since we always
                    // have RetryDelegatingHandler as the outer-most handler
                    while (handler!.InnerHandler is DelegatingHandler)
                    {
                        handler = handler.InnerHandler as DelegatingHandler;
                    }

                    handler.InnerHandler = currentHandler;

                    currentHandler = handlers[i];
                }
            }

            return currentHandler;
        }
    }
}
