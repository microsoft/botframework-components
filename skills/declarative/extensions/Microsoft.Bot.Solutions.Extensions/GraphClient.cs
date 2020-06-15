using Microsoft.Graph;
using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Net.Http;
using System.Threading;

namespace Microsoft.Bot.Solutions.Extensions
{
    public class GraphClient
    {
        public static GraphServiceClient GetAuthenticatedClient(string accessToken, HttpMessageHandler httpHandler = null)
        {
            if (httpHandler != null)
            {
                var authProvider = new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        // Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);

                        // Get event times in the current time zone.
                        requestMessage.Headers.Add("Prefer", "outlook.timezone=\"" + TimeZoneInfo.Utc.Id + "\"");
                        await Task.CompletedTask;
                    });

                var handlers = GraphClientFactory.CreateDefaultHandlers(authProvider);

                var httpClient = GraphClientFactory.Create(handlers, finalHandler: httpHandler);

                return new GraphServiceClient(httpClient);
            }

            var graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        // Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);

                        // Get event times in the current time zone.
                        requestMessage.Headers.Add("Prefer", "outlook.timezone=\"" + TimeZoneInfo.Utc.Id + "\"");
                        await Task.CompletedTask;
                    }));
            return graphClient;
        }

        public static Exception HandleGraphAPIException(ServiceException ex)
        {
            return new Exception($"Microsoft Graph API Exception: {ex.Message}");
        }
    }
}