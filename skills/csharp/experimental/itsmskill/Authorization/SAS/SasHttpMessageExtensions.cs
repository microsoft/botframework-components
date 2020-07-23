using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Web;

namespace ITSMSkill.Authorization.SAS
{
    public static class SASHttpMessageExtensions
    {
        /// <summary>
        /// Indicates if the request contains SAS credentials.
        /// </summary>
        /// <param name="request">The request.</param>
        public static bool ContainsSasCredentials(this HttpRequestMessage request)
        {
            var credentials = request.GetSasCredentials();

            return
                credentials != null
                && credentials.Policy != null
                && credentials.Policy.Permissions != null
                && !string.IsNullOrEmpty(credentials.Policy.Permissions.ToString())
                && !string.IsNullOrEmpty(credentials.Policy.Version)
                && !string.IsNullOrEmpty(credentials.Signature);
        }

        /// <summary>
        /// Gets SAS credentials.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>SharedAccessCredentials.</returns>
        public static SharedAccessCredentials GetSasCredentials(this HttpRequestMessage request)
        {
            var policy = new SharedAccessPolicy
            {
                StartTime = GetQueryTime(request: request, queryParameter: SharedAccessCredentials.SasStartTimeQueryParameter),
                ExpireTime = GetQueryTime(request: request, queryParameter: SharedAccessCredentials.SasExpireTimeQueryParameter),
                Version = GetSingleOrDefaultQuerySafely(request: request, name: SharedAccessCredentials.SasVersionQueryParamater, defaultValue: string.Empty),
                Permissions = SharedAccessPermissions.FromString(input: GetSingleOrDefaultQuerySafely(request: request, name: SharedAccessCredentials.SasPermissionsQueryParameter, defaultValue: string.Empty)),
            };

            return new SharedAccessCredentials(
                policy: policy,
                signature: GetSingleOrDefaultQuerySafely(request: request, name: SharedAccessCredentials.SasSignatureQueryParameter, defaultValue: string.Empty));
        }

        /// <summary>
        /// Gets the key value collection of query parameters.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>IEnumerable<KeyValuePair<string, string>>.</returns>
        public static IEnumerable<KeyValuePair<string, string>> GetQueryNameValuePairs(this HttpRequestMessage request)
        {
            var queryParams = request.RequestUri.ParseQueryString();
            return queryParams.Cast<string>().Select(key => new KeyValuePair<string, string>(key, queryParams[key]));
        }

        /// <summary>
        /// Gets the single or default query safely.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        public static string GetSingleOrDefaultQuerySafely(this HttpRequestMessage request, string name, string defaultValue = null)
        {
            var values = request.GetQueryNameValuePairs().Where(queryKvp => queryKvp.Key.Equals(name, StringComparison.OrdinalIgnoreCase))
                .Select(queryKvp => queryKvp.Value).ToArray<string>();
            return values.Length == 1 ? values.Single() : defaultValue;
        }

        /// <summary>
        /// Gets the query value or default.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <param name="name">The query name.</param>
        /// <param name="defaultValue">The default query value.</param>
        public static string GetQueryOrDefault(this HttpRequestMessage request, string name, string defaultValue)
        {
            return request?.RequestUri?.Query != null
                ? HttpUtility.ParseQueryString(request.RequestUri.Query)[name] ?? defaultValue
                : defaultValue;
        }

        /// <summary>
        /// Gets query time.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="queryParameter">The query parameter.</param>
        private static DateTime? GetQueryTime(HttpRequestMessage request, string queryParameter)
        {
            return DateTime.TryParse(
                s: request.GetQueryOrDefault(queryParameter, string.Empty),
                provider: null,
                styles: DateTimeStyles.RoundtripKind,
                result: out DateTime time)
                    ? (DateTime?)time
                    : null;
        }
    }
}
