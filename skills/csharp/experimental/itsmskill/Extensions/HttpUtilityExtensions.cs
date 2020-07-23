// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;

namespace ITSMSkill.Extensions
{
    public static class HttpUtilityExtensions
    {
        /// <summary>
        /// Returns true if the status code corresponds to a successful request.
        /// </summary>
        /// <param name="statusCode">The status code.</param>
        /// <returns>bool.</returns>
        public static bool IsSuccessfulRequest(this HttpStatusCode statusCode) =>
            (statusCode >= HttpStatusCode.OK && (int)statusCode <= 299)
            || statusCode == HttpStatusCode.NotModified
            || statusCode == HttpStatusCode.Redirect
            || statusCode == HttpStatusCode.MovedPermanently;
    }
}
