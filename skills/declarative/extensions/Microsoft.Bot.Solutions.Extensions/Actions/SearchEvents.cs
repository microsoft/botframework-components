extern alias BetaLib;
using Beta = BetaLib.Microsoft.Graph;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Solutions.Extensions.Actions
{
    public class SearchEvents : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.SearchEvents";

        [JsonConstructor]
        public SearchEvents([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        [JsonProperty("startProperty")]
        public ObjectExpression<DateTime?> StartProperty { get; set; }

        [JsonProperty("endProperty")]
        public ObjectExpression<DateTime?> EndProperty { get; set; }

        [JsonProperty("timeZoneProperty")]
        public StringExpression TimeZoneProperty { get; set; }

        [JsonProperty("subjectProperty")]
        public StringExpression SubjectProperty { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var token = Token.GetValue(dcState);
            var startProperty = StartProperty.GetValue(dcState);
            var endProperty = EndProperty.GetValue(dcState);
            var timeZoneProperty = TimeZoneProperty.GetValue(dcState);
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneProperty);


            var graphClient = BetaGraphClient.GetAuthenticatedClient(token);

            var requests = new List<Beta.SearchRequestObject>()
            {
                new Beta.SearchRequestObject()
                {
                    EntityTypes = new List<Beta.EntityType>()
                    {
                        Beta.EntityType.Event
                    },
                    Query = new Beta.SearchQuery()
                    {
                        Query_string = new Beta.SearchQueryString { Query = "from:darren" }
                    }
                }
            };
            var results = await graphClient.Search.Query(requests).Request().PostAsync();
            

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(GetEvents), results, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, results);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: results, cancellationToken: cancellationToken);
        }
    }
}
