using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Azure.Search.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Extensions.Actions
{
    class GetMeetingRooms : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.AzureSearch.Calendar.GetMeetingRooms";

        [JsonConstructor]
        public GetMeetingRooms([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("searchServiceAdminApiKey")]
        public StringExpression SearchServiceAdminApiKey { get; set; }

        [JsonProperty("searchServiceName")]
        public StringExpression SearchServiceName { get; set; }

        [JsonProperty("searchIndexName")]
        public StringExpression SearchIndexName { get; set; }

        [JsonProperty("building")]
        public StringExpression Building { get; set; }

        [JsonProperty("floorNumber")]
        public IntExpression FloorNumber { get; set; }

        [JsonProperty("meetingRoom")]
        public StringExpression MeetingRoom { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var searchServiceAdminApiKey = SearchServiceAdminApiKey.GetValue(dcState);
            var searchServiceName = SearchServiceName.GetValue(dcState);
            var searchIndexName = SearchIndexName.GetValue(dcState);
            var building = Building.GetValue(dcState);
            var floorNumber = FloorNumber.GetValue(dcState);
            var meetingRoom = MeetingRoom.GetValue(dcState);
            
            var searchQuery = "*";
            if (building != null && meetingRoom != null)
            {
                searchQuery = building + "* " + meetingRoom;
            }
            else if (building != null || meetingRoom != null)
            {
                searchQuery = (building ?? meetingRoom) + "* ";
            }

            var searchParameters = new SearchParameters()
            {
                Top = meetingRoom != null ? 1 : 10,
                Filter = floorNumber == 0? null : "FloorNumber eq " + floorNumber
            };
            var httpHandler = dc.Context.TurnState.Get<HttpMessageHandler>();
            var azureSearchClient = AzureSearchClient.GetAzureSearchClient(searchServiceName, searchServiceAdminApiKey, searchIndexName);

            var searchResult = await azureSearchClient.Documents.SearchWithHttpMessagesAsync(searchQuery, searchParameters);

            var result = searchResult.Body.Results.Select(x => x.Document);

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(GetMeetingRooms), result, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, result);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: result, cancellationToken: cancellationToken);
        }
    }
}
