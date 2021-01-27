﻿using Microsoft.Graph;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Microsoft.BotFramework.Composer.CustomAction
{
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
            return new Exception($"Microsoft Graph API Exception: {ex.Message}");
        }
    }
}