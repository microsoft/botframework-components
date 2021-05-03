// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.Graph.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.TraceExtensions;
    using Microsoft.Bot.Components.Graph;
    using Microsoft.Graph;

    /// <summary>
    /// Base MSGraph custom action that returns a typed parameter.
    /// </summary>
    /// <typeparam name="T">The type of the return parameter.</typeparam>
    public abstract class BaseMsGraphCustomAction<T> : BaseMsGraphCustomAction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseMsGraphCustomAction{T}"/> class.
        /// </summary>
        /// <param name="callerPath">Caller path.</param>
        /// <param name="callerLine">Caller line.</param>
        public BaseMsGraphCustomAction(string callerPath = "", int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        /// <summary>
        /// Custom action implementation.
        /// </summary>
        /// <param name="dc">Context of the current dialog.</param>
        /// <param name="options">Any additional options needed.</param>
        /// <param name="cancellationToken">Cancelation token for the async task.</param>
        /// <returns>Dialog result.</returns>
        public override async Task<DialogTurnResult> BeginDialogAsync(
            DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            string token = this.Token.GetValue(dc.State);
            HttpClient httpClient = dc.Context.TurnState.Get<HttpClient>();

            T result = default(T);

            if (httpClient != null)
            {
                result = await CallGraphInternalAsync(dc, token, httpClient, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                using (httpClient = new HttpClient())
                {
                    result = await CallGraphInternalAsync(dc, token, httpClient, cancellationToken).ConfigureAwait(false);
                }
            }

            // Write Trace Activity for the http request and response values
            await dc.Context.TraceActivityAsync(
                name: this.DeclarativeType,
                value: null,
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
            return await dc.EndDialogAsync(result: result, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc />
        internal sealed override Task CallGraphServiceAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            // Do nothing because this version of the base custom action is typed
            // and it won't be called anyway. This method is purposefully sealed
            // and throws so we do not execute it.
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calls the graph service.
        /// </summary>
        /// <param name="client">Instance of <see cref="IGraphServiceClient"/>.</param>
        /// <param name="parameters">Parameters for the call.</param>
        /// <param name="cancellationToken">Cancellation token for the async call.</param>
        /// <returns>Task for the async operation.</returns>
        internal abstract Task<T> CallGraphServiceWithResultAsync(
            IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken);

        private async Task<T> CallGraphInternalAsync(DialogContext dc, string token, HttpClient httpClient, CancellationToken cancellationToken)
        {
            IGraphServiceClient graphClient = MSGraphClient.GetAuthenticatedClient(token, httpClient);

            var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            this.PopulateParameters(dc.State, parameters);

            T result = default(T);

            Stopwatch sw = new Stopwatch();
            Exception exCaught = null;

            try
            {
                sw.Start();
                result = await this.CallGraphServiceWithResultAsync(graphClient, parameters, cancellationToken).ConfigureAwait(false);
            }
            catch (ServiceException ex)
            {
                exCaught = ex;

                this.HandleServiceException(ex);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                exCaught = ex;
            }
            finally
            {
                sw.Stop();

                this.FireTelemetryEvent(sw.ElapsedMilliseconds, exCaught);
            }

            return result;
        }
    }
}
