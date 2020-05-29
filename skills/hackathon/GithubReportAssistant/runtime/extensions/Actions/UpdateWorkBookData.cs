using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Solutions.Extensions.Common;
using Microsoft.Graph;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Solutions.Extensions.Actions
{
    public class UpdateWorkBookData : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Excel.UpdateWorkData";

        [JsonConstructor]
        public UpdateWorkBookData([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        [JsonProperty("xArray")]
        public ArrayExpression<string> XArray { get; set; }

        [JsonProperty("yArray")]
        public ArrayExpression<string> YArray { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var token = Token.GetValue(dcState);
            var xArray = XArray.GetValue(dcState);
            var yArray = YArray.GetValue(dcState);

            try
            {
                var graphClient = GraphClient.GetAuthenticatedClient(token);

                var searchResults = await GraphHelper.GetFilesAsync(graphClient);//SearchFilesAsync(graphClient, "Book");
                if (searchResults.Count() > 0)
                {
                    var id = searchResults[0].Id;
                    var persistChanges = true;
                    var sessionInfo = await GraphHelper.CreateSession(graphClient, id, persistChanges);

                    var workbookRange = await GraphHelper.GetRange(graphClient, id, "Sheet1", "A1:B"+ xArray.Count(), sessionInfo);

                    var values = workbookRange.Values;

                    for (int i = 0; i < xArray.Count(); i++)
                    {
                        values[i][0] = xArray[i];
                    }

                    for (int i = 0; i < yArray.Count(); i++)
                    {
                        values[i][1] = yArray[i];
                    }

                    var workbookRangeUpdated = await GraphHelper.PatchRange(graphClient, id, "Sheet1", "A1:B" + xArray.Count(), workbookRange, sessionInfo);
                    await GraphHelper.CloseSession(graphClient, id, sessionInfo);
                }
            }
            catch (ServiceException ex)
            {
                throw GraphClient.HandleGraphAPIException(ex);
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(UpdateWorkBookData), null, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: null, cancellationToken: cancellationToken);
        }
    }
}
