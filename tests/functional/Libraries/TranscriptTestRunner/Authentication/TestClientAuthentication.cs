// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TranscriptTestRunner.Authentication
{
    /// <summary>
    /// Authentication class for the Test Client.
    /// </summary>
    public static class TestClientAuthentication
    {
        /// <summary>
        /// Signs in to the bot.
        /// </summary>
        /// <param name="url">The sign in Url.</param>
        /// <param name="originHeader">The Origin Header with key and value.</param>
        /// <param name="sessionInfo">The Session information definition.</param>
        /// <returns>True, if SignIn is successful; otherwise false.</returns>
        public static async Task<bool> SignInAsync(string url, KeyValuePair<string, string> originHeader, SessionInfo sessionInfo)
        {
            var cookieContainer = new CookieContainer();
            using var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                CookieContainer = cookieContainer
            };

            // We have a sign in url, which will produce multiple HTTP 302 for redirects
            // This will path 
            //      token service -> other services -> auth provider -> token service (post sign in)-> response with token
            // When we receive the post sign in redirect, we add the cookie passed in the session info
            // to test enhanced authentication. This in the scenarios happens by itself since browsers do this for us.
            using var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add(originHeader.Key, originHeader.Value);

            while (!string.IsNullOrEmpty(url))
            {
                using var response = await client.GetAsync(new Uri(url)).ConfigureAwait(false);
                var text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

                url = response.StatusCode == HttpStatusCode.Redirect
                    ? response.Headers.Location.OriginalString
                    : null;

                // Once the redirects are done, there is no more url. 
                // This means we did the entire loop.
                if (url == null)
                {
                    if (!response.IsSuccessStatusCode || !text.Contains("You are now signed in and can close this window."))
                    {
                        throw new InvalidOperationException("An error occurred signing in");
                    }

                    return true;
                }

                // If this is the post sign in callback, add the cookie and code challenge
                // so that the token service gets the verification.
                // Here we are simulating what WebChat does along with the browser cookies.
                if (url.StartsWith("https://token.botframework.com/api/oauth/PostSignInCallback", StringComparison.Ordinal))
                {
                    url += $"&code_challenge={sessionInfo.SessionId}";
                    cookieContainer.Add(sessionInfo.Cookie);
                }
            }

            throw new InvalidOperationException("Sign in did not succeed. Set a breakpoint in TestClientAuthentication.SignInAsync() to debug the redirect sequence.");
        }

        /// <summary>
        /// Obtains the <see cref="SessionInfo"/> from an URL endpoint with a token.
        /// </summary>
        /// <param name="url">The URL endpoint to obtain the <see cref="SessionInfo"/> from.</param>
        /// <param name="token">The token to use for authorization.</param>
        /// <param name="originHeader">The Origin Header with key and value.</param>
        /// <returns>The <see cref="SessionInfo"/> Task.</returns>
        public static async Task<SessionInfo> GetSessionInfoAsync(string url, string token, KeyValuePair<string, string> originHeader)
        {
            // Set up cookie container to obtain response cookie
            var cookies = new CookieContainer();
            using var handler = new HttpClientHandler { CookieContainer = cookies };

            using var client = new HttpClient(handler);

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // We want to add the Origins header to this client as well
            client.DefaultRequestHeaders.Add(originHeader.Key, originHeader.Value);

            using var response = await client.SendAsync(request).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                // Extract cookie from cookies
                var cookie = cookies.GetCookies(new Uri(url)).Cast<Cookie>().FirstOrDefault(c => c.Name == "webchat_session_v2");

                // Extract session info from body
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                var session = JsonConvert.DeserializeObject<Session>(body);

                return new SessionInfo
                {
                    SessionId = session.SessionId,
                    Cookie = cookie
                };
            }

            throw new InvalidOperationException("Failed to obtain session id");
        }
    }
}
