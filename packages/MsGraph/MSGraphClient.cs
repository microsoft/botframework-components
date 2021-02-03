// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.MsGraph
{
    using System;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Microsoft.Graph;

    /// <summary>
    /// Client to call MS Graph service
    /// </summary>
    public class MSGraphClient
    {
        public static GraphServiceClient GetAuthenticatedClient(string accessToken, HttpClient httpClient)
        {
            var client = new GraphServiceClient(httpClient);
            client.AuthenticationProvider = new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        // Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);

                        // Get event times in the current time zone.
                        requestMessage.Headers.Add("Prefer", "outlook.timezone=\"" + TimeZoneInfo.Utc.Id + "\"");
                        await Task.CompletedTask;
                    });

            return client;
        }

        public static Exception HandleGraphAPIException(ServiceException ex)
        {
            // TODO: Why do we throw a very generic exception when there is a much better
            // exception to use?
            return new Exception($"Microsoft Graph API Exception: {ex.Message}", ex);
        }
    }
}