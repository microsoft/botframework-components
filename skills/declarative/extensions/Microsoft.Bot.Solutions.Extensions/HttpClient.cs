using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Microsoft.Bot.Solutions.Extensions
{
    public static class HttpClient
    {
        public static System.Net.Http.HttpClient GetAuthenticatedClient(string accessToken)
        {
            var httpClient = new System.Net.Http.HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return httpClient;
        }

        public static Exception HandleGraphAPIException(Exception ex)
        {
            return new Exception($"Microsoft Graph API Exception: {ex.Message}");
        }
    }
}
