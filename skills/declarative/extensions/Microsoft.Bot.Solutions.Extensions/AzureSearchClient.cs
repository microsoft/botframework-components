using Microsoft.Azure.Search;
using System.Net.Http;

namespace Microsoft.Bot.Solutions.Extensions
{
    class AzureSearchClient
    {
        public static ISearchIndexClient GetAzureSearchClient(string searchServiceName, string searchServiceAdminApiKey, string searchIndexName, HttpMessageHandler httpHandler = null)
        {
            if (httpHandler != null)
            {
                return new SearchIndexClient(new SearchCredentials(searchServiceAdminApiKey), new System.Net.Http.HttpClient(httpHandler), true)
                {
                    SearchServiceName = searchServiceName,
                    IndexName = searchIndexName
                };
            }

            ISearchServiceClient searchClient = new SearchServiceClient(searchServiceName, new SearchCredentials(searchServiceAdminApiKey));
            return searchClient.Indexes.GetClient(searchIndexName);
        }
    }
}
