using Microsoft.Azure.Search;

namespace Microsoft.Bot.Solutions.Extensions
{
    class AzureSearchClient
    {
        public static ISearchIndexClient GetAzureSearchClient(string searchServiceName, string searchServiceAdminApiKey, string searchIndexName)
        {
            ISearchServiceClient searchClient = new SearchServiceClient(searchServiceName, new SearchCredentials(searchServiceAdminApiKey));
            return searchClient.Indexes.GetClient(searchIndexName);
        }
    }
}
