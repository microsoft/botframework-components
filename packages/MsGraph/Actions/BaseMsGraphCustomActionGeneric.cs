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
    /// Base MSGraph custom action that returns a typed parameter
    /// </summary>
    /// <typeparam name="T">The type of the return parameter</typeparam>
    public abstract class BaseMsGraphCustomAction<T> : BaseMsGraphCustomAction
    {
        public BaseMsGraphCustomAction(string callerPath = "", int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        /// <summary>
        /// Custom action implementation
        /// </summary>
        /// <param name="dc">Context of the current dialog</param>
        /// <param name="options">Any additional options needed.</param>
        /// <param name="cancellationToken">Cancelation token for the async task</param>
        /// <returns></returns>
        public override async Task<DialogTurnResult> BeginDialogAsync(
            DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            string token = this.Token.GetValue(dc.State);
            HttpClient httpClient = dc.Context.TurnState.Get<HttpClient>() ?? new HttpClient();
            GraphServiceClient graphClient = MSGraphClient.GetAuthenticatedClient(token, httpClient);

            T result = default(T);

            try
            {
                result = await this.CallGraphServiceWithResultAsync(graphClient, dc, cancellationToken);
            }
            catch (ServiceException ex)
            {
                throw MSGraphClient.HandleGraphAPIException(ex);
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(
                name: this.DeclarativeType,
                value: result,
                valueType: this.DeclarativeType,
                label: this.DeclarativeType,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (this.ResultProperty != null)
            {
                // Internally SetValue already calls JToken.FromObject to convert into a
                // JObject so there is no need for us to set it.
                dc.State.SetValue(this.ResultProperty.GetValue(dc.State), result);
            }

            // return the actionResult as the result of this operation
            return await dc.EndDialogAsync(result: result, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Abstract method to call graph service
        /// </summary>
        /// <returns></returns>
        protected abstract Task<T> CallGraphServiceWithResultAsync(
            GraphServiceClient client, DialogContext dc, CancellationToken cancellationToken);

        protected sealed override Task CallGraphServiceAsync(GraphServiceClient client, DialogContext dc, CancellationToken cancellationToken)
        {
            // Do nothing because this version of the base custom action is typed
            // and it won't be called anyway. This method is purposefully sealed
            // and throws so we do not execute it.
            throw new NotImplementedException();
        }
    }
}
