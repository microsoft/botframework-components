using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace GenericITSMSkill.Extensions
{
    public static class HttpUtilityExtensions
    {
        /// <summary>
        /// Returns true if the status code corresponds to a successful request.
        /// </summary>
        /// <param name="statusCode">The status code.</param>
        public static bool IsSuccessfulRequest(this HttpStatusCode statusCode) =>
            (statusCode >= HttpStatusCode.OK && (int)statusCode <= 299)
            || statusCode == HttpStatusCode.NotModified
            || statusCode == HttpStatusCode.Redirect
            || statusCode == HttpStatusCode.MovedPermanently;
    }
}
