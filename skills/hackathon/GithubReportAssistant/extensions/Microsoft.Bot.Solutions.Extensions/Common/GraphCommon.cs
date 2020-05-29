using Microsoft.Graph;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Solutions.Extensions.Common
{
    public static class GraphHelper
    {
        /// <summary>
        /// Get Files From Root folder
        /// </summary>
        /// <returns>
        /// DriveItemChildrenCollectionPage Result
        /// </returns>
        public static async Task<IDriveItemChildrenCollectionPage> GetFilesAsync(GraphServiceClient graphClient)
        {
            var files = await graphClient.Me.Drive.Root.Children
                .Request()
                .GetAsync();

            return files;
        }

        /// <summary>
        /// Search Files From Root folder
        /// </summary>
        /// <returns>
        /// DriveItemChildrenCollectionPage Result
        /// </returns>
        public static async Task<IDriveItemSearchCollectionPage> SearchFilesAsync(GraphServiceClient graphClient, string fileName)
        {
            var files = await graphClient.Me.Drive.Root.Search(fileName)
                .Request()
                .GetAsync();

            return files;
        }

        public static async Task<Workbook> GetWorkbook(GraphServiceClient graphClient, string idItem)
        {
            var workBook = await graphClient.Me.Drive.Items[idItem].Workbook
                .Request()
                .GetAsync();

            return workBook;
        }

        public static async Task<WorkbookSessionInfo> CreateSession(GraphServiceClient graphClient, string idItem, bool persistChanges)
        {
            var sessionInfo = await graphClient.Me.Drive.Items[idItem].Workbook
                .CreateSession(persistChanges)
                .Request()
                .PostAsync();

            return sessionInfo;
        }

        public static async Task CloseSession(GraphServiceClient graphClient, string idItem, WorkbookSessionInfo workbookSessionInfo)
        {
            var optionSession = new HeaderOption("workbook-session-id", workbookSessionInfo.Id);
            var options = new List<Option> { optionSession };
            await graphClient.Me.Drive.Items[idItem].Workbook
                .CloseSession()
                .Request(options)
                .PostAsync();
        }

        public static async Task RefreshSession(GraphServiceClient graphClient, string idItem, WorkbookSessionInfo workbookSessionInfo)
        {
            var optionSession = new HeaderOption("workbook-session-id", workbookSessionInfo.Id);
            var options = new List<Option> { optionSession };
            await graphClient.Me.Drive.Items[idItem].Workbook
                .RefreshSession()
                .Request(options)
                .PostAsync();
        }

        public static async Task<IEnumerable<WorkbookWorksheet>> GetSheets(GraphServiceClient graphClient, string idItem)
        {
            var sheets = await graphClient.Me.Drive.Items[idItem].Workbook.Worksheets
                .Request()
                .GetAsync();

            return sheets.CurrentPage.AsEnumerable();
        }

        public static async Task<WorkbookRange> GetRange(GraphServiceClient graphClient, string idItem, string nameSheet, string range, WorkbookSessionInfo workbookSessionInfo)
        {
            var name = nameof(WorkbookRange.Values);
            var optionSession = new HeaderOption("workbook-session-id", workbookSessionInfo.Id);
            var options = new List<Option> { optionSession };
            var workbookRange = await graphClient.Me.Drive.Items[idItem].Workbook
                .Worksheets[nameSheet]
                .Range(range)
                .Request(options)
                .Select(name)
                .GetAsync();

            return workbookRange;
        }

        public static async Task<WorkbookRange> PatchRange(GraphServiceClient graphClient, string idItem, string nameSheet, string range, WorkbookRange workbookRangeUpdate, WorkbookSessionInfo workbookSessionInfo)
        {
            //var name = nameof(WorkbookRange.Values);
            var optionSession = new HeaderOption("workbook-session-id", workbookSessionInfo.Id);
            var options = new List<Option> { optionSession };
            var workbookRange = await graphClient.Me.Drive.Items[idItem].Workbook.Worksheets[nameSheet]
                .Range(range)
                .Request(options)
                .PatchAsync(workbookRangeUpdate);

            return workbookRange;
        }

        public static async Task<WorkbookRange> PutRange(GraphServiceClient graphClient, string idItem, string nameSheet, string range, WorkbookRange workbookRangeUpdate, WorkbookSessionInfo workbookSessionInfo)
        {
            var optionSession = new HeaderOption("workbook-session-id", workbookSessionInfo.Id);
            var options = new List<Option> { optionSession };
            var workbookRange = await graphClient.Me.Drive.Items[idItem].Workbook.Worksheets[nameSheet]
                .Range(range)
                .Request(options)
                .Select("Values")
                .PutAsync(workbookRangeUpdate);

            return workbookRange;
        }

        public static async Task<IWorkbookTablesCollectionPage> GetTablesAsync(GraphServiceClient graphClient, string idItem)
        {
            var workbookTable = await graphClient.Me.Drive.Items[idItem].Workbook.Tables
                .Request()
                .GetAsync();

            return workbookTable;
        }

        public static async Task<WorkbookRange> GetCell(GraphServiceClient graphClient, string idItem, string nameSheet, int row, int column, WorkbookSessionInfo workbookSessionInfo)
        {
            var optionSession = new HeaderOption("workbook-session-id", workbookSessionInfo.Id);
            var options = new List<Option> { optionSession };
            var workbookRange = await graphClient.Me.Drive.Items[idItem].Workbook.Worksheets[nameSheet]
                .Cell(row, column)
                .Request(options)
                .Select("Values")
                .GetAsync();

            return workbookRange;
        }

        public static async Task<WorkbookRange> PatchCell(GraphServiceClient graphClient, string idItem, string nameSheet, int row, int column, WorkbookRange workbookRangeUpdate, WorkbookSessionInfo workbookSessionInfo)
        {
            var optionSession = new HeaderOption("workbook-session-id", workbookSessionInfo.Id);
            var options = new List<Option> { optionSession };
            var workbookRange = await graphClient.Me.Drive.Items[idItem].Workbook.Worksheets[nameSheet]
                .Cell(row, column)
                .Request(options)
                //.Select("Values")
                .PatchAsync(workbookRangeUpdate);

            return workbookRange;
        }

        public static async Task<WorkbookRange> PutCell(GraphServiceClient graphClient, string idItem, string nameSheet, int row, int column, WorkbookRange workbookRangeUpdate, WorkbookSessionInfo workbookSessionInfo)
        {
            var optionSession = new HeaderOption("workbook-session-id", workbookSessionInfo.Id);
            var options = new List<Option> { optionSession };
            var workbookRange = await graphClient.Me.Drive.Items[idItem].Workbook.Worksheets[nameSheet]
                .Cell(row, column)
                .Request(options)
                //.Select(nameof(workbookRangeUpdate.Values))
                .PutAsync(workbookRangeUpdate);

            return workbookRange;
        }

        public static async Task<IWorkbookTableColumnsCollectionPage> GetColumn(GraphServiceClient graphClient, string idItem, string nameSheet)
        {
            var columns = await graphClient.Me.Drive.Items[idItem].Workbook.Tables[nameSheet].Columns
                .Request()
                //.Skip(5)
                .Top(2)
                .GetAsync();
            return columns;
        }

        public static async Task<string> GetChart(GraphServiceClient graphClient, string idItem, string nameSheet, string chartName)
        {
            var chartResourceUrl = graphClient.Me.Drive.Items[idItem]
             .Workbook
             .Worksheets[nameSheet]
             .Charts[chartName]
             .Request().RequestUrl;

            string imageResource = string.Empty;

            var urlToGetImageFromChart = $"{chartResourceUrl}/image(width=400, height=480)";
            var message = new HttpRequestMessage(HttpMethod.Get, urlToGetImageFromChart);
            await graphClient.AuthenticationProvider.AuthenticateRequestAsync(message);
            var response = await graphClient.HttpProvider.SendAsync(message);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                JObject imageObject = JObject.Parse(content);
                JToken chartData = imageObject.GetValue("value");
                imageResource = chartData.ToString();
            }


            return imageResource;
        }

        //public static async Task<WorkbookRangeInsertRequest> InsertRange(string idItem, string nameSheet, string range)
        //{
        //    var graphClient = AuthenticationHelper.GetAuthenticatedClient();
        //    var shift = "Down";

        //    var workbookRangeInsert = await graphClient.Drive.Items[idItem].Workbook.Worksheets[nameSheet]
        //        .Range(range)
        //        .Request()
        //        .PatchAsync();

        //    return workbookRangeInsert;
        //}


    }

}


