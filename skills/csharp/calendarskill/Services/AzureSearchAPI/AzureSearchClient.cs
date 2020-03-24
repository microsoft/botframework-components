using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalendarSkill.Models;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Rest.Azure;

namespace CalendarSkill.Services.AzureSearchAPI
{
    public class AzureSearchClient : ISearchService
    {
        private static ISearchIndexClient _indexClient;

        public AzureSearchClient(string searchServiceName, string searchServiceAdminApiKey, string searchIndexName)
        {
            ISearchServiceClient searchClient = new SearchServiceClient(searchServiceName, new SearchCredentials(searchServiceAdminApiKey));
            _indexClient = searchClient.Indexes.GetClient(searchIndexName);
        }

        public async Task<List<RoomModel>> GetMeetingRoomAsync(string meetingRoom = null, string building = null, int floorNumber = 0)
        {
            // Enable fuzzy match, and search top50 candidates if given building but just top1 if given room name.
            string query = "*";
            int topN = 1;
            if (!string.IsNullOrEmpty(meetingRoom) && !string.IsNullOrEmpty(building))
            {
                query = building + "* " + meetingRoom;
            }
            else if (!string.IsNullOrEmpty(meetingRoom))
            {
                query = meetingRoom + "*";
            }
            else if (!string.IsNullOrEmpty(building))
            {
                query = building + "*";
                topN = 50;
            }

            return await GetMeetingRoomAsync(query, floorNumber, topN);
        }

        private async Task<List<RoomModel>> GetMeetingRoomAsync(string query, int floorNumber = 0, int topN = 50)
        {
            List<RoomModel> meetingRooms = new List<RoomModel>();

            DocumentSearchResult<RoomModel> searchResults = await SearchMeetingRoomAsync(SearchMode.All, query, floorNumber, topN);

            if (searchResults.Results.Count() == 0)
            {
                searchResults = await SearchMeetingRoomAsync(SearchMode.Any, query, floorNumber, topN);
            }

            foreach (SearchResult<RoomModel> result in searchResults.Results)
            {
                // Only EmailAddress is required and we will use it to book the room.
                if (string.IsNullOrEmpty(result.Document.EmailAddress))
                {
                    throw new Exception("EmailAddress of meeting room is null");
                }

                if (string.IsNullOrEmpty(result.Document.DisplayName))
                {
                    result.Document.DisplayName = result.Document.EmailAddress;
                }

                meetingRooms.Add(result.Document);
            }

            return meetingRooms;
        }

        private static SkillException HandleAzureSearchException(Exception ex)
        {
            var skillExceptionType = SkillExceptionType.Other;
            if (ex is CloudException)
            {
                var cex = ex as CloudException;
                if (cex.Response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    skillExceptionType = SkillExceptionType.APIForbidden;
                }
            }

            return new SkillException(skillExceptionType, ex.Message, ex);
        }

        private async Task<DocumentSearchResult<RoomModel>> SearchMeetingRoomAsync(SearchMode searchMode, string query, int floorNumber = 0, int topN = 50)
        {
            SearchParameters parameters = new SearchParameters()
            {
                SearchMode = searchMode,
                Filter = floorNumber == 0 ? null : SearchFilters.FloorNumberFilter + floorNumber.ToString(),
                Top = topN
            };
            try
            {
                DocumentSearchResult<RoomModel> searchResults = await _indexClient.Documents.SearchAsync<RoomModel>(query, parameters);
                return searchResults;
            }
            catch (Exception ex)
            {
                throw HandleAzureSearchException(ex);
            }
        }

        private class SearchFilters
        {
            public const string FloorNumberFilter = "FloorNumber eq ";
        }
    }
}
