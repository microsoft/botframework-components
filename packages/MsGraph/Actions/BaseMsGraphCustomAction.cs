// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
namespace Microsoft.Bot.Component.MsGraph.Actions.MSGraph
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveExpressions.Properties;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.TraceExtensions;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// Base class for any custom actions that calls to MSGraph
    /// </summary>
    public abstract class BaseMsGraphCustomAction : Dialog
    {
        /// <summary>
        /// Creates an instance of <seealso cref="BaseMsGraphEvent" />
        /// </summary>
        /// <param name="callerPath"></param>
        /// <param name="callerLine"></param>
        public BaseMsGraphCustomAction(string callerPath = "", int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        /// <summary>
        /// Gets the path to which we store the result from the output of this custom action
        /// </summary>
        [JsonProperty("resultProperty")]
        public StringExpression ResultProperty { get; set; }

        /// <summary>
        /// Authentication token used by the internal <see cref="GraphServiceClient" /> to call
        /// MSGraph service
        /// </summary>
        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        /// <summary>
        /// Gets the declarative of the custom action
        /// </summary>
        /// <value></value>
        [JsonProperty("$kind")]
        public abstract string DeclarativeType { get; }

        public override async Task<DialogTurnResult> BeginDialogAsync(
            DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            string token = this.Token.GetValue(dc.State);
            HttpClient httpClient = dc.Context.TurnState.Get<HttpClient>() ?? new HttpClient();
            GraphServiceClient graphClient = MSGraphClient.GetAuthenticatedClient(token, httpClient);

            try
            {
                await this.CallGraphServiceAsync(graphClient, dc, cancellationToken);
            }
            catch (ServiceException ex)
            {
                throw MSGraphClient.HandleGraphAPIException(ex);
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(this.DeclarativeType, null, valueType: this.DeclarativeType, label: this.DeclarativeType).ConfigureAwait(false);

            var result = true;

            if (this.ResultProperty != null)
            {
                dc.State.SetValue(this.ResultProperty.GetValue(dc.State), result);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: result, cancellationToken: cancellationToken);
        }

        protected abstract Task CallGraphServiceAsync(GraphServiceClient client, DialogContext dc, CancellationToken cancellationToken);
    }
}
