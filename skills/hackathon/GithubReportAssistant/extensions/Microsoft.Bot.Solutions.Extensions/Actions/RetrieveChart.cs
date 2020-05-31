using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Solutions.Extensions.Common;
using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Solutions.Extensions.Actions
{
    public class RetrieveChart : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Excel.RetrieveChart";

        [JsonConstructor]
        public RetrieveChart([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        [JsonProperty("sheetName")]
        public StringExpression SheetName { get; set; }

        [JsonProperty("chartName")]
        public StringExpression ChartName { get; set; }


        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var token = Token.GetValue(dcState);
            var sheetName = SheetName.GetValue(dcState);
            var chartName = ChartName.GetValue(dcState);
            var graphClient = GraphClient.GetAuthenticatedClient(token);

            string chartResource = string.Empty;
            try
            {
                var searchResults = await GraphHelper.GetFilesAsync(graphClient);//SearchFilesAsync(graphClient, "Book");
                if (searchResults.Count() > 0)
                {
                    var id = String.Empty;
                    foreach (var searchResult in searchResults)
                    {
                        if (searchResult.Name.ToLower().Contains("book") && !searchResult.Name.ToLower().Contains("notebook"))
                        {
                            id = searchResult.Id;
                            break;
                        }
                    }

                    var querySheetName = (sheetName == null) ? "Sheet1" : sheetName;
                    var queryChartName = (chartName == null) ? "Chart 2" : chartName;

                    var persistChanges = true;
                    var sessionInfo = await GraphHelper.CreateSession(graphClient, id, persistChanges);
                    chartResource = await GraphHelper.GetChart(graphClient, id, querySheetName, queryChartName);
                    
                    await GraphHelper.CloseSession(graphClient, id, sessionInfo);
                }
            }
            catch (ServiceException)
            {
                return null;
            }

            var results = string.Format("data:image/png;base64,{0}", chartResource);
            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(RetrieveChart), results, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, results);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: results, cancellationToken: cancellationToken);
        }

        private byte[] ReadFully(Stream input)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                input.CopyTo(ms);
                return ms.ToArray();
            }
        }
    }
}
