using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Components.Recognizers.CLURecognizer.CLU
{
    /// <inheritdoc />
    /// <summary>
    /// A CLU based implementation of <see cref="ITelemetryRecognizer"/>.
    /// </summary>
    public class CluMainRecognizer : ITelemetryRecognizer
    {
        private readonly CluRecognizerOptionsBase _recognizerOptions;
        private readonly IHttpClientFactory _clientFactory;
        private readonly string _cacheKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="CluMainRecognizer"/> class.
        /// </summary>
        /// <param name="recognizerOptions"> The CLU recognizer version options.</param>
        /// <param name="clientFactory">The HttpClient factory for the CLU API calls.</param>
        public CluMainRecognizer(CluRecognizerOptionsBase recognizerOptions, IHttpClientFactory clientFactory)
        {
            TelemetryClient = recognizerOptions.TelemetryClient;
            LogPersonalInformation = recognizerOptions.LogPersonalInformation;
            _recognizerOptions = recognizerOptions;
            _clientFactory = clientFactory;
            _cacheKey = _recognizerOptions.Application.Endpoint + _recognizerOptions.Application.ProjectName;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to log personal information that came from the user to telemetry.
        /// </summary>
        /// <value>If true, personal information is logged to Telemetry; otherwise the properties will be filtered.</value>
        public bool LogPersonalInformation { get; set; }

        /// <summary>
        /// Gets the currently configured <see cref="IBotTelemetryClient"/> that logs the CluResult event.
        /// </summary>
        /// <value>The <see cref="IBotTelemetryClient"/> being used to log events.</value>
        [JsonIgnore]
        public IBotTelemetryClient TelemetryClient { get; }

        /// <inheritdoc />
        public virtual async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CancellationToken cancellationToken)
            => await RecognizeInternalAsync(turnContext, null, null, null, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Runs an utterance through a recognizer and returns a generic recognizer result.
        /// </summary>
        /// <param name="dialogContext">dialogcontext.</param>
        /// <param name="activity">activity.</param>
        /// <param name="cancellationToken">cancellationtoken.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public virtual async Task<RecognizerResult> RecognizeAsync(DialogContext dialogContext, Activity activity, CancellationToken cancellationToken)
            => await RecognizeInternalAsync(dialogContext, activity, null, null, null, cancellationToken).ConfigureAwait(false);

        /// <inheritdoc />
        public virtual async Task<T> RecognizeAsync<T>(ITurnContext turnContext, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
        {
            var result = new T();

            result.Convert(await RecognizeInternalAsync(turnContext, null, null, null, cancellationToken).ConfigureAwait(false));

            return result;
        }

        /// <summary>
        /// Runs an utterance through a recognizer and returns a strongly-typed recognizer result.
        /// </summary>
        /// <typeparam name="T">type of result.</typeparam>
        /// <param name="dialogContext">dialogContext.</param>
        /// <param name="activity">activity.</param>
        /// <param name="cancellationToken">cancellationToken.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public virtual async Task<T> RecognizeAsync<T>(DialogContext dialogContext, Activity activity, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
        {
            var result = new T();

            result.Convert(await RecognizeInternalAsync(dialogContext, activity, null, null, null, cancellationToken).ConfigureAwait(false));

            return result;
        }

        /// <summary>
        /// Return results of the analysis (Suggested actions and intents).
        /// </summary>
        /// <param name="turnContext">Context object containing information for a single turn of conversation with a user.</param>
        /// <param name="telemetryProperties">Additional properties to be logged to telemetry with the CluResult event.</param>
        /// <param name="telemetryMetrics">Additional metrics to be logged to telemetry with the CluResult event.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The CLU results of the analysis of the current message text in the current turn's context activity.</returns>
        public virtual async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, Dictionary<string, string> telemetryProperties, Dictionary<string, double>? telemetryMetrics, CancellationToken cancellationToken = default)
            => await RecognizeInternalAsync(turnContext, null, telemetryProperties, telemetryMetrics, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Return results of the analysis (Suggested actions and intents).
        /// </summary>
        /// <param name="dialogContext">Context object containing information for a single turn of conversation with a user.</param>
        /// <param name="activity">activity to recognize.</param>
        /// <param name="telemetryProperties">Additional properties to be logged to telemetry with the CluResult event.</param>
        /// <param name="telemetryMetrics">Additional metrics to be logged to telemetry with the CluResult event.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The CLU results of the analysis of the current message text in the current turn's context activity.</returns>
        public virtual async Task<RecognizerResult> RecognizeAsync(DialogContext dialogContext, Activity activity, Dictionary<string, string> telemetryProperties, Dictionary<string, double>? telemetryMetrics, CancellationToken cancellationToken = default)
            => await RecognizeInternalAsync(dialogContext, activity, null, telemetryProperties, telemetryMetrics, cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Return results of the analysis (Suggested actions and intents).
        /// </summary>
        /// <typeparam name="T">The recognition result type.</typeparam>
        /// <param name="turnContext">Context object containing information for a single turn of conversation with a user.</param>
        /// <param name="telemetryProperties">Additional properties to be logged to telemetry with the CluResult event.</param>
        /// <param name="telemetryMetrics">Additional metrics to be logged to telemetry with the CluResult event.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The CLU results of the analysis of the current message text in the current turn's context activity.</returns>
        public virtual async Task<T> RecognizeAsync<T>(ITurnContext turnContext, Dictionary<string, string> telemetryProperties, Dictionary<string, double>? telemetryMetrics, CancellationToken cancellationToken = default)
            where T : IRecognizerConvert, new()
        {
            var result = new T();

            result.Convert(await RecognizeInternalAsync(turnContext, null, telemetryProperties, telemetryMetrics, cancellationToken).ConfigureAwait(false));

            return result;
        }

        /// <summary>
        /// Return results of the analysis (Suggested actions and intents).
        /// </summary>
        /// <typeparam name="T">The recognition result type.</typeparam>
        /// <param name="dialogContext">Context object containing information for a single turn of conversation with a user.</param>
        /// <param name="activity">activity to recognize.</param>
        /// <param name="telemetryProperties">Additional properties to be logged to telemetry with the CluResult event.</param>
        /// <param name="telemetryMetrics">Additional metrics to be logged to telemetry with the CluResult event.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The CLU results of the analysis of the current message text in the current turn's context activity.</returns>
        public virtual async Task<T> RecognizeAsync<T>(DialogContext dialogContext, Activity activity, Dictionary<string, string> telemetryProperties, Dictionary<string, double>? telemetryMetrics, CancellationToken cancellationToken = default)
            where T : IRecognizerConvert, new()
        {
            var result = new T();

            result.Convert(await RecognizeInternalAsync(dialogContext, activity, null, telemetryProperties, telemetryMetrics, cancellationToken).ConfigureAwait(false));

            return result;
        }

        /// <summary>
        /// Runs an utterance through a recognizer and returns a generic recognizer result.
        /// </summary>
        /// <param name="turnContext">Turn context.</param>
        /// <param name="recognizerOptions">A <see cref="CluRecognizerOptionsBase"/> instance to be used by the call.
        /// This parameter overrides the default <see cref="CluRecognizerOptionsBase"/> passed in the constructor.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Analysis of utterance.</returns>
        public virtual async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CluRecognizerOptionsBase recognizerOptions, CancellationToken cancellationToken)
        {
            return await RecognizeInternalAsync(turnContext, recognizerOptions, null, null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Runs an utterance through a recognizer and returns a generic recognizer result.
        /// </summary>
        /// <param name="dialogContext">dialog context.</param>
        /// <param name="activity">activity to recognize.</param>
        /// <param name="recognizerOptions">A <see cref="CluRecognizerOptionsBase"/> instance to be used by the call.
        /// This parameter overrides the default <see cref="CluRecognizerOptionsBase"/> passed in the constructor.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Analysis of utterance.</returns>
        public virtual async Task<RecognizerResult> RecognizeAsync(DialogContext dialogContext, Activity activity, CluRecognizerOptionsBase recognizerOptions, CancellationToken cancellationToken)
        {
            return await RecognizeInternalAsync(dialogContext, activity, recognizerOptions, null, null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Runs an utterance through a recognizer and returns a strongly-typed recognizer result.
        /// </summary>
        /// <typeparam name="T">The recognition result type.</typeparam>
        /// <param name="turnContext">Turn context.</param>
        /// <param name="recognizerOptions">A <see cref="CluRecognizerOptionsBase"/> instance to be used by the call.
        /// This parameter overrides the default <see cref="CluRecognizerOptionsBase"/> passed in the constructor.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Analysis of utterance.</returns>
        public virtual async Task<T> RecognizeAsync<T>(ITurnContext turnContext, CluRecognizerOptionsBase recognizerOptions, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
        {
            var result = new T();

            result.Convert(await RecognizeInternalAsync(turnContext, recognizerOptions, null, null, cancellationToken).ConfigureAwait(false));

            return result;
        }

        /// <summary>
        /// Runs an utterance through a recognizer and returns a strongly-typed recognizer result.
        /// </summary>
        /// <typeparam name="T">The recognition result type.</typeparam>
        /// <param name="dialogContext">dialog context.</param>
        /// <param name="activity">activity to recognize.</param>
        /// <param name="recognizerOptions">A <see cref="CluRecognizerOptionsBase"/> instance to be used by the call.
        /// This parameter overrides the default <see cref="CluRecognizerOptionsBase"/> passed in the constructor.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Analysis of utterance.</returns>
        public virtual async Task<T> RecognizeAsync<T>(DialogContext dialogContext, Activity activity, CluRecognizerOptionsBase recognizerOptions, CancellationToken cancellationToken)
            where T : IRecognizerConvert, new()
        {
            var result = new T();

            result.Convert(await RecognizeInternalAsync(dialogContext, activity, recognizerOptions, null, null, cancellationToken).ConfigureAwait(false));

            return result;
        }

        /// <summary>
        /// Return results of the analysis (Suggested actions and intents).
        /// </summary>
        /// <param name="turnContext">Context object containing information for a single turn of conversation with a user.</param>
        /// <param name="recognizerOptions">A <see cref="CluRecognizerOptionsBase"/> instance to be used by the call.
        /// This parameter overrides the default <see cref="CluRecognizerOptionsBase"/> passed in the constructor.</param>
        /// <param name="telemetryProperties">Additional properties to be logged to telemetry with the CluResult event.</param>
        /// <param name="telemetryMetrics">Additional metrics to be logged to telemetry with the CluResult event.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The CLU results of the analysis of the current message text in the current turn's context activity.</returns>
        public virtual async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, CluRecognizerOptionsBase recognizerOptions, Dictionary<string, string> telemetryProperties, Dictionary<string, double>? telemetryMetrics, CancellationToken cancellationToken = default)
        {
            return await RecognizeInternalAsync(turnContext, recognizerOptions, telemetryProperties, telemetryMetrics, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Return results of the analysis (Suggested actions and intents).
        /// </summary>
        /// <param name="dialogContext">Context object containing information for a single turn of conversation with a user.</param>
        /// <param name="activity">activity to recognize.</param>
        /// <param name="recognizerOptions">A <see cref="CluRecognizerOptionsBase"/> instance to be used by the call.
        /// This parameter overrides the default <see cref="CluRecognizerOptionsBase"/> passed in the constructor.</param>
        /// <param name="telemetryProperties">Additional properties to be logged to telemetry with the CluResult event.</param>
        /// <param name="telemetryMetrics">Additional metrics to be logged to telemetry with the CluResult event.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The CLU results of the analysis of the current message text in the current turn's context activity.</returns>
        public virtual async Task<RecognizerResult> RecognizeAsync(DialogContext dialogContext, Activity activity, CluRecognizerOptionsBase recognizerOptions, Dictionary<string, string> telemetryProperties, Dictionary<string, double>? telemetryMetrics, CancellationToken cancellationToken = default)
        {
            return await RecognizeInternalAsync(dialogContext, activity, recognizerOptions, telemetryProperties, telemetryMetrics, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Return results of the analysis (Suggested actions and intents).
        /// </summary>
        /// <typeparam name="T">The recognition result type.</typeparam>
        /// <param name="turnContext">Context object containing information for a single turn of conversation with a user.</param>
        /// <param name="recognizerOptions">A <see cref="CluRecognizerOptionsBase"/> instance to be used by the call.
        /// This parameter overrides the default <see cref="CluRecognizerOptionsBase"/> passed in the constructor.</param>
        /// <param name="telemetryProperties">Additional properties to be logged to telemetry with the CluResult event.</param>
        /// <param name="telemetryMetrics">Additional metrics to be logged to telemetry with the CluResult event.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The CLU results of the analysis of the current message text in the current turn's context activity.</returns>
        public virtual async Task<T> RecognizeAsync<T>(ITurnContext turnContext, CluRecognizerOptionsBase recognizerOptions, Dictionary<string, string> telemetryProperties, Dictionary<string, double>? telemetryMetrics, CancellationToken cancellationToken = default)
            where T : IRecognizerConvert, new()
        {
            var result = new T();

            result.Convert(await RecognizeInternalAsync(turnContext, recognizerOptions, telemetryProperties, telemetryMetrics, cancellationToken).ConfigureAwait(false));

            return result;
        }

        /// <summary>
        /// Return results of the analysis (Suggested actions and intents).
        /// </summary>
        /// <typeparam name="T">The recognition result type.</typeparam>
        /// <param name="dialogContext">Context object containing information for a single turn of conversation with a user.</param>
        /// <param name="activity">activity to recognize.</param>
        /// <param name="recognizerOptions">A <see cref="CluRecognizerOptionsBase"/> instance to be used by the call.
        /// This parameter overrides the default <see cref="CluRecognizerOptionsBase"/> passed in the constructor.</param>
        /// <param name="telemetryProperties">Additional properties to be logged to telemetry with the CluResult event.</param>
        /// <param name="telemetryMetrics">Additional metrics to be logged to telemetry with the CluResult event.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The CLU results of the analysis of the current message text in the current turn's context activity.</returns>
        public virtual async Task<T> RecognizeAsync<T>(DialogContext dialogContext, Activity activity, CluRecognizerOptionsBase recognizerOptions, Dictionary<string, string> telemetryProperties, Dictionary<string, double>? telemetryMetrics, CancellationToken cancellationToken = default)
            where T : IRecognizerConvert, new()
        {
            var result = new T();

            result.Convert(await RecognizeInternalAsync(dialogContext, activity, recognizerOptions, telemetryProperties, telemetryMetrics, cancellationToken).ConfigureAwait(false));

            return result;
        }

        /// <summary>
        /// Return results of the analysis (Suggested actions and intents).
        /// </summary>
        /// <remarks>No telemetry is provided when using this method.</remarks>
        /// <param name="utterance">utterance to recognize.</param>
        /// <param name="recognizerOptions">A <see cref="CluRecognizerOptionsBase"/> instance to be used by the call.
        /// This parameter overrides the default <see cref="CluRecognizerOptionsBase"/> passed in the constructor.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>The CLU results of the analysis of the current message text in the current turn's context activity.</returns>
        public virtual async Task<RecognizerResult> RecognizeAsync(string utterance, CluRecognizerOptionsBase? recognizerOptions, CancellationToken cancellationToken = default)
        {
            recognizerOptions ??= _recognizerOptions;

            return await RecognizeInternalAsync(utterance, recognizerOptions, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Invoked prior to a CluResult being logged.
        /// </summary>
        /// <param name="recognizerResult">The Luis Results for the call.</param>
        /// <param name="turnContext">Context object containing information for a single turn of conversation with a user.</param>
        /// <param name="telemetryProperties">Additional properties to be logged to telemetry with the CluResult event.</param>
        /// <param name="telemetryMetrics">Additional metrics to be logged to telemetry with the CluResult event.</param>
        protected virtual void OnRecognizerResult(RecognizerResult recognizerResult, ITurnContext turnContext, Dictionary<string, string>? telemetryProperties, Dictionary<string, double>? telemetryMetrics)
        {
            // Track the event
            _recognizerOptions.TelemetryClient.TrackEvent(CluConstants.Telemetry.CluResult, FillCluEventProperties(recognizerResult, turnContext, telemetryProperties), telemetryMetrics);
        }

        /// <summary>
        /// Fills the event properties for CluResult event for telemetry.
        /// These properties are logged when the recognizer is called.
        /// </summary>
        /// <param name="recognizerResult">Last activity sent from user.</param>
        /// <param name="turnContext">Context object containing information for a single turn of conversation with a user.</param>
        /// <param name="telemetryProperties">Additional properties to be logged to telemetry with the CluResult event.</param>
        /// additionalProperties
        /// <returns>A dictionary that is sent as "Properties" to IBotTelemetryClient.TrackEvent method for the BotMessageSend event.</returns>
        protected Dictionary<string, string> FillCluEventProperties(RecognizerResult recognizerResult, ITurnContext turnContext, Dictionary<string, string>? telemetryProperties)
        {
            var topTwoIntents = (recognizerResult.Intents.Count > 0) ? recognizerResult.Intents.OrderByDescending(x => x.Value.Score).Take(2).ToArray() : null;

            // Add the intent score and conversation id properties
            var properties = new Dictionary<string, string>()
            {
                { CluConstants.Telemetry.ProjectNameProperty, _recognizerOptions.Application.ProjectName },
                { CluConstants.Telemetry.IntentProperty, topTwoIntents?[0].Key ?? string.Empty },
                { CluConstants.Telemetry.IntentScoreProperty, topTwoIntents?[0].Value.Score?.ToString("N2", CultureInfo.InvariantCulture) ?? "0.00" },
                { CluConstants.Telemetry.Intent2Property, (topTwoIntents?.Length > 1) ? topTwoIntents?[1].Key ?? string.Empty : string.Empty },
                { CluConstants.Telemetry.IntentScore2Property, (topTwoIntents?.Length > 1) ? topTwoIntents?[1].Value.Score?.ToString("N2", CultureInfo.InvariantCulture) ?? "0.00" : "0.00" },
                { CluConstants.Telemetry.FromIdProperty, turnContext.Activity.From.Id },
            };

            var entities = recognizerResult.Entities?.ToString();

            if (!string.IsNullOrWhiteSpace(entities))
            {
                properties.Add(CluConstants.Telemetry.EntitiesProperty, entities!);
            }

            // Use the LogPersonalInformation flag to toggle logging PII data, text is a common example
            if (LogPersonalInformation && !string.IsNullOrEmpty(turnContext.Activity.Text))
            {
                properties.Add(CluConstants.Telemetry.QuestionProperty, turnContext.Activity.Text);
            }

            // Additional Properties can override "stock" properties.
            if (telemetryProperties != null)
            {
                return telemetryProperties
                    .Concat(properties)
                    .GroupBy(kv => kv.Key)
                    .ToDictionary(g => g.Key, g => g.First().Value);
            }

            return properties;
        }

        /// <summary>
        /// Returns a RecognizerResult object.
        /// </summary>
        /// <param name="turnContext">Dialog turn Context.</param>
        /// <param name="predictionOptions">CluRecognizerOptions implementation to override current properties.</param>
        /// <param name="telemetryProperties"> Additional properties to be logged to telemetry with the CluResult event.</param>
        /// <param name="telemetryMetrics">Additional metrics to be logged to telemetry with the CluResult event.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>RecognizerResult object.</returns>
        private async Task<RecognizerResult> RecognizeInternalAsync(ITurnContext turnContext, CluRecognizerOptionsBase? predictionOptions, Dictionary<string, string>? telemetryProperties, Dictionary<string, double>? telemetryMetrics, CancellationToken cancellationToken)
        {
            var recognizer = predictionOptions ?? _recognizerOptions;

            var cached = turnContext.TurnState.Get<RecognizerResult>(_cacheKey);

            if (cached == null)
            {
                var result = await recognizer.RecognizeInternalAsync(turnContext, _clientFactory.CreateClient(recognizer.LogicalHttpClientName), cancellationToken).ConfigureAwait(false);

                OnRecognizerResult(result, turnContext, telemetryProperties, telemetryMetrics);

                turnContext.TurnState.Set(_cacheKey, result);

                _recognizerOptions.TelemetryClient.TrackEvent(CluConstants.TrackEventOptions.ResultCachedEventName, telemetryProperties, telemetryMetrics);

                return result;
            }

            _recognizerOptions.TelemetryClient.TrackEvent(CluConstants.TrackEventOptions.ReadFromCachedResultEventName, telemetryProperties, telemetryMetrics);

            return cached;
        }

        /// <summary>
        /// Returns a RecognizerResult object.
        /// </summary>
        /// <param name="dialogContext">Dialog turn Context.</param>
        /// <param name="activity">activity to recognize.</param>
        /// <param name="predictionOptions">CluRecognizerOptions implementation to override current properties.</param>
        /// <param name="telemetryProperties"> Additional properties to be logged to telemetry with the CluResult event.</param>
        /// <param name="telemetryMetrics">Additional metrics to be logged to telemetry with the CluResult event.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>RecognizerResult object.</returns>
        private async Task<RecognizerResult> RecognizeInternalAsync(DialogContext dialogContext, Activity activity, CluRecognizerOptionsBase? predictionOptions, Dictionary<string, string>? telemetryProperties, Dictionary<string, double>? telemetryMetrics, CancellationToken cancellationToken)
        {
            var recognizer = predictionOptions ?? _recognizerOptions;
            var turnContext = dialogContext.Context;
            var cached = turnContext.TurnState.Get<RecognizerResult>(_cacheKey);

            if (cached == null)
            {
                var result = await recognizer.RecognizeInternalAsync(dialogContext, activity, _clientFactory.CreateClient(recognizer.LogicalHttpClientName), cancellationToken).ConfigureAwait(false);

                OnRecognizerResult(result, dialogContext.Context, telemetryProperties, telemetryMetrics);

                turnContext.TurnState.Set(_cacheKey, result);

                _recognizerOptions.TelemetryClient.TrackEvent(CluConstants.TrackEventOptions.ResultCachedEventName, telemetryProperties, telemetryMetrics);

                return result;
            }

            _recognizerOptions.TelemetryClient.TrackEvent(CluConstants.TrackEventOptions.ReadFromCachedResultEventName, telemetryProperties, telemetryMetrics);

            return cached;
        }

        /// <summary>
        /// Returns a RecognizerResult object.
        /// </summary>
        /// <param name="utterance">utterance to recognize.</param>
        /// <param name="predictionOptions">CluRecognizerOptions implementation to override current properties.</param>
        /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        /// <returns>RecognizerResult object.</returns>
        private async Task<RecognizerResult> RecognizeInternalAsync(string utterance, CluRecognizerOptionsBase? predictionOptions, CancellationToken cancellationToken)
        {
            var recognizer = predictionOptions ?? _recognizerOptions;

            var result = await recognizer.RecognizeInternalAsync(utterance, _clientFactory.CreateClient(recognizer.LogicalHttpClientName), cancellationToken).ConfigureAwait(false);

            return result;
        }
    }
}
