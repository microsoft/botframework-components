// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace ITSMSkill.Extensions
{
    public static class HttpRequestMessageExtensions
    {
        /// <summary>
        /// Returns an individual querystring value.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="key"></param>
        /// <returns>string.</returns>
        public static string GetQueryString(this HttpRequestMessage request, string key)
        {
            // IEnumerable<KeyValuePair<string,string>>
            var queryStrings = request.GetQueryNameValuePairs();
            if (queryStrings == null)
            {
                return null;
            }

            var match = queryStrings.First(kv => string.Compare(kv.Key, key, true) == 0);
            if (string.IsNullOrEmpty(match.Value))
            {
                return null;
            }

            return match.Value;
        }

        public static IEnumerable<KeyValuePair<string, string>> GetQueryNameValuePairs(this HttpRequestMessage request)
        {
            var queryParams = request.RequestUri.ParseQueryString();
            return queryParams.Cast<string>().Select(key => new KeyValuePair<string, string>(key, queryParams[key]));
        }
    }
}
