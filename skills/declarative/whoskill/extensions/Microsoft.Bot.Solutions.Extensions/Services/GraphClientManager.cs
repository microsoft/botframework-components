using Microsoft.Graph;
using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.Bot.Solutions.Extensions.Services
{
    public class GraphClientManager
    {
        public static GraphServiceClient GetAuthenticatedClient(string accessToken)
        {
            var graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        // Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);

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
