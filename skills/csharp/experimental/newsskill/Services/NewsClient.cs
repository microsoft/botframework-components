// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Search.NewsSearch;
using Microsoft.Azure.CognitiveServices.Search.NewsSearch.Models;

namespace NewsSkill.Services
{
    public class HttpUrlRewriteHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, System.Threading.CancellationToken cancellationToken)
        {
            // Rewriting the URI because for the V7 version of the API there is a
            // migration from Cognitive Service to Bing API URL. However, the relative path
            // is not the same. So while we can set the EndPoint in the NewsClient we cannot
            // set the relative path for the REST calls. Doing this to effectively hijack
            // the request and rewrite the URI to get to the right place.
            if (request.RequestUri.Host.Contains("api.bing.microsoft.com") &&
                request.RequestUri.AbsolutePath.Contains("bing/v7.0/news"))
            {
                Uri newUri = new Uri(request.RequestUri.AbsoluteUri.Replace("bing/v7.0/news", "v7.0/news"));

                request.RequestUri = newUri;
            }

            return base.SendAsync(request, cancellationToken);
        }
    }

    public class NewsClient : IDisposable
    {
        private NewsSearchClient _client;

        public NewsClient(string endPoint, string key)
        {
            _client = new NewsSearchClient(new ApiKeyServiceClientCredentials(key), new HttpUrlRewriteHandler());

            if (!string.IsNullOrEmpty(endPoint))
            {
                _client.Endpoint = endPoint;
            }
        }

        public async Task<IList<NewsArticle>> GetNewsForTopicAsync(string query, string mkt)
        {
            // general search by topic
            var results = await _client.News.SearchAsync(query, countryCode: mkt, count: 10);
            return results.Value;
        }

        public async Task<IList<NewsTopic>> GetTrendingNewsAsync(string mkt)
        {
            // get articles trending on social media
            var results = await _client.News.TrendingAsync(countryCode: mkt, count: 10);
            return results.Value;
        }

        // see for valid categories: https://docs.microsoft.com/en-us/rest/api/cognitiveservices-bingsearch/bing-news-api-v7-reference#news-categories-by-market
        public async Task<IList<NewsArticle>> GetNewsByCategoryAsync(string topic, string mkt)
        {
            // general search by category
            var results = await _client.News.CategoryAsync(category: topic, market: mkt, count: 10);
            return results.Value;
        }

        /// <summary>
        /// Dispose of the news http client and all the objects underneath
        /// </summary>
        public void Dispose()
        {
            this._client.Dispose();
        }
    }
}
