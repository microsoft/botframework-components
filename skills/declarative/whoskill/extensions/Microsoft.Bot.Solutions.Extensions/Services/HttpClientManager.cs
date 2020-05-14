using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Microsoft.Bot.Solutions.Extensions.Services
{
    public static class HttpClientManager
    {
        public static HttpClient GetAuthenticatedClient(string accessToken)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return httpClient;
        }

        public static Exception HandleGraphAPIException(Exception ex)
        {
            return new Exception($"Microsoft Graph API Exception: {ex.Message}");
        }
    }
}
