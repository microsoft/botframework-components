using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Graph;
using Newtonsoft.Json;

namespace Microsoft.BotFramework.Composer.CustomAction.Actions.MSGraph
{
    class TentativelyAcceptEvent : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Graph.Calendar.TentativelyAcceptEvent";

        [JsonConstructor]
        public TentativelyAcceptEvent([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        [JsonProperty("resultProperty")]
        public string ResultProperty { get; set; }

        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        [JsonProperty("eventId")]
        public StringExpression EventId { get; set; }

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            var dcState = dc.State;
            var token = Token.GetValue(dcState);
            var eventId = EventId.GetValue(dcState);

            var httpHandler = dc.Context.TurnState.Get<HttpMessageHandler>();

            var httpClient = dc.Context.TurnState.Get<HttpClient>() ?? new HttpClient();
            var graphClient = MSGraphClient.GetAuthenticatedClient(token, httpClient);

            
            try
            {
                await graphClient.Me.Events[eventId].TentativelyAccept("tentativelyAccept").Request().PostAsync();
            }
            catch (ServiceException ex)
            {
                throw MSGraphClient.HandleGraphAPIException(ex);
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(nameof(AcceptEvent), null, valueType: DeclarativeType, label: this.Id).ConfigureAwait(false);

            var result = true;

            if (this.ResultProperty != null)
            {
                dcState.SetValue(ResultProperty, result);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: result, cancellationToken: cancellationToken);
        }
    }
}
