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
    using AdaptiveExpressions.Properties;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Builder.Dialogs.Memory;
    using Microsoft.Bot.Builder.TraceExtensions;
    using Microsoft.Bot.Components.Graph;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// Base class for any custom actions that calls to MSGraph.
    /// </summary>
    public abstract class BaseMsGraphCustomAction : Dialog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseMsGraphCustomAction"/> class.
        /// </summary>
        /// <param name="callerPath">Caller path.</param>
        /// <param name="callerLine">Caller line.</param>
        public BaseMsGraphCustomAction(string callerPath = "", int callerLine = 0)
            : base()
        {
            this.RegisterSourceLocation(callerPath, callerLine);
        }

        /// <summary>
        /// Gets or sets the path to which we store the result from the output of this custom action.
        /// </summary>
        [JsonProperty("resultProperty")]
        public StringExpression ResultProperty { get; set; }

        /// <summary>
        /// Gets or sets the authentication token used by the internal <see cref="GraphServiceClient" /> to call
        /// MSGraph service.
        /// </summary>
        [JsonProperty("token")]
        public StringExpression Token { get; set; }

        /// <summary>
        /// Gets the declarative of the custom action.
        /// </summary>
        [JsonProperty("$kind")]
        public abstract string DeclarativeType { get; }

        /// <summary>
        /// Gets the telemetry event name.
        /// </summary>
        [JsonIgnore]
        public string TelemetryEventName
        {
            get
            {
                return $"Graph_{this.DeclarativeType}";
            }
        }

        /// <summary>
        /// Gets the latency property name in the custom metrics property in App Insight.
        /// </summary>
        [JsonIgnore]
        public string TelemetryLatencyPropertyName
        {
            get
            {
                return $"GraphLatency_{this.DeclarativeType}";
            }
        }

        /// <summary>
        /// Gets the exception proeprty name in the custom property in App Insight.
        /// </summary>
        [JsonIgnore]
        public string TelemetryExceptionPropertyName
        {
            get
            {
                return $"GraphException_{this.DeclarativeType}";
            }
        }

        /// <summary>
        /// Beings the dialog.
        /// </summary>
        /// <param name="dc">Dialog context.</param>
        /// <param name="options">Options for the dialog.</param>
        /// <param name="cancellationToken">Cancellation token for async operations.</param>
        /// <returns>Dialog result.</returns>
        public override async Task<DialogTurnResult> BeginDialogAsync(
            DialogContext dc, object options = null, CancellationToken cancellationToken = default)
        {
            string token = this.Token.GetValue(dc.State);
            HttpClient httpClient = dc.Context.TurnState.Get<HttpClient>() ?? new HttpClient();
            IGraphServiceClient graphClient = MSGraphClient.GetAuthenticatedClient(token, httpClient);

            var parameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            this.PopulateParameters(dc.State, parameters);

            Stopwatch sw = new Stopwatch();
            Exception exCaught = null;

            try
            {
                sw.Start();
                await this.CallGraphServiceAsync(graphClient, parameters, cancellationToken);
            }
            catch (ServiceException ex)
            {
                exCaught = ex;

                this.HandleServiceException(ex);
            }
            catch (Exception ex)
            {
                exCaught = ex;
            }
            finally
            {
                sw.Stop();

                this.FireTelemetryEvent(sw.ElapsedMilliseconds, exCaught);
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

        /// <summary>
        /// Calls the graph service.
        /// </summary>
        /// <param name="client">Instance of <see cref="IGraphServiceClient"/>.</param>
        /// <param name="parameters">Parameters for the call.</param>
        /// <param name="cancellationToken">Cancellation token for the async call.</param>
        /// <returns>Task for the async operation.</returns>
        internal abstract Task CallGraphServiceAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken);

        /// <summary>
        /// Populates the dictionary of parameter for the operation to run.
        /// </summary>
        /// <param name="state">Dialog state.</param>
        /// <param name="parameters">Dictionary of the parameters.</param>
        protected virtual void PopulateParameters(DialogStateManager state, Dictionary<string, object> parameters)
        {
            // Do nothing in default implementation.
        }

        /// <summary>
        /// Fires the telemetry event to App Insight.
        /// </summary>
        /// <param name="latency">Latency of the call.</param>
        /// <param name="exCaught">The exception caught.</param>
        protected void FireTelemetryEvent(long latency, Exception exCaught)
        {
            try
            {
                var metric = new Dictionary<string, double>();
                metric.Add(this.TelemetryLatencyPropertyName, Convert.ToDouble(latency));

                var properties = new Dictionary<string, string>();

                if (exCaught != null)
                {
                    properties.Add($"{this.TelemetryExceptionPropertyName}-Message", exCaught.Message);
                    properties.Add($"{this.TelemetryExceptionPropertyName}-StackTrace", exCaught.StackTrace);
                    properties.Add($"{this.TelemetryExceptionPropertyName}-ExceptionType", exCaught.GetType().FullName);
                }

                // Log the latency and any exception that we caught.
                // Telemetry client is something that is handled by the base Dialog class, and by extension the SDK
                // platform itself. If the bot has telemetry enabled with the instrumentation key, then this would
                // automatically show up on their Application Insight monitoring log events.
                this.TelemetryClient.TrackEvent(this.TelemetryEventName, properties, metric);
            }
            catch
            {
                // If exception is found, don't do anything to crash the bot. This isn't quite that important.
            }
        }

        /// <summary>
        /// Handler for <see cref="ServiceException"/>.
        /// </summary>
        /// <param name="ex">Exception to handle.</param>
        protected virtual void HandleServiceException(ServiceException ex)
        {
            throw MSGraphClient.HandleGraphAPIException(ex);
        }
    }
}
