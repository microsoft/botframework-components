using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Components.Recognizers.CLURecognizer;
using Microsoft.Bot.Components.Recognizers.CLURecognizer.CLU;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Components.Recognizers
{
    public class CluAdaptiveRecognizer : Recognizer
    {
        /// <summary>
        /// The declarative type for this recognizer.
        /// </summary>
        [JsonProperty("$kind")]
        public const string Kind = "Microsoft.CluRecognizer";

        /// <summary>
        /// Gets or sets the ProjectName of your Conversation Language Understanding service.
        /// </summary>
        /// <value>
        /// The project name of your Conversation Language Understanding service.
        /// </value>
        [JsonProperty("projectName")]
        public StringExpression ProjectName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the Endpoint for your Conversation Language Understanding service.
        /// </summary>
        /// <value>
        /// The endpoint of your Conversation Language Understanding service.
        /// </value>
        [JsonProperty("endpoint")]
        public StringExpression Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the EndpointKey for your Conversation Language Understanding service.
        /// </summary>
        /// <value>
        /// The endpoint key for your Conversation Language Understanding service.
        /// </value>
        [JsonProperty("endpointKey")]
        public StringExpression EndpointKey { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the DeploymentName for your Conversation Language Understanding service.
        /// </summary>
        /// <value>
        /// The deployment name for your Conversation Language Understanding service.
        /// </value>
        [JsonProperty("deploymentName")]
        public StringExpression DeploymentName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the flag to determine if personal information should be logged in telemetry.
        /// </summary>
        /// <value>
        /// The flag to indicate in personal information should be logged in telemetry.
        /// </value>
        [JsonProperty("logPersonalInformation")]
        public BoolExpression LogPersonalInformation { get; set; } = "=settings.runtimeSettings.telemetry.logPersonalInformation";

        /// <summary>
        /// Gets or sets a value indicating whether API results should be included.
        /// </summary>
        /// <value>True to include API results.</value>
        /// <remarks>This is mainly useful for testing or getting access to CLU features not yet in the SDK.</remarks>
        [JsonProperty("includeAPIResults")]
        public BoolExpression IncludeAPIResults { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating the string index type to include in the the CLU request body.
        /// </summary>
        /// <value>
        /// A value indicating the string index type to include in the the CLU request body.
        /// </value>
        [JsonProperty("cluRequestBodyStringIndexType")]
        public StringExpression CluRequestBodyStringIndexType { get; set; } = CluConstants.RequestOptions.StringIndexType;

        /// <summary>
        /// Gets or sets a value indicating the CLU API version to use.
        /// This can be helpful combined with the <see cref="IncludeAPIResults"/> flag to get access to CLU features not yet in the SDK.
        /// </summary>
        /// <value>
        /// A value indicating the CLU API version to use.
        /// </value>
        [JsonProperty("cluApiVersion")]
        public StringExpression CluApiVersion { get; set; } = CluConstants.RequestOptions.ApiVersion;

        /// <inheritdoc/>
        public override async Task<RecognizerResult> RecognizeAsync(DialogContext dialogContext, Activity activity, CancellationToken cancellationToken = default, Dictionary<string, string>? telemetryProperties = null, Dictionary<string, double>? telemetryMetrics = null)
        {
            var recognizer = new CluMainRecognizer(RecognizerOptions(dialogContext), new DefaultHttpClientFactory());

            var result = await recognizer.RecognizeAsync(dialogContext, activity, cancellationToken).ConfigureAwait(false);

            TrackRecognizerResult(dialogContext, CluConstants.TrackEventOptions.RecognizerResultEventName, FillRecognizerResultTelemetryProperties(result, telemetryProperties, dialogContext), telemetryMetrics);

            return result;
        }

        /// <summary>
        /// Construct recognizer options from the current dialog context.
        /// </summary>
        /// <param name="dialogContext">Context.</param>
        /// <returns>CLU Recognizer options.</returns>
        public CluRecognizerOptions RecognizerOptions(DialogContext dialogContext)
        {
            var application = new CluApplication(ProjectName.GetValue(dialogContext.State), EndpointKey.GetValue(dialogContext.State), Endpoint.GetValue(dialogContext.State), DeploymentName.GetValue(dialogContext.State));

            return new CluRecognizerOptions(application)
            {
                TelemetryClient = TelemetryClient,
                LogPersonalInformation = LogPersonalInformation.GetValue(dialogContext.State),
                IncludeAPIResults = IncludeAPIResults.GetValue(dialogContext.State),
                CluRequestBodyStringIndexType = CluRequestBodyStringIndexType.GetValue(dialogContext.State),
                CluApiVersion = CluApiVersion.GetValue(dialogContext.State),
                LogicalHttpClientName = CluConstants.HttpClientOptions.DefaultLogicalName,
            };
        }

        /// <summary>
        /// Uses the <see cref="RecognizerResult"/> returned from the <see cref="CluMainRecognizer"/> and populates a dictionary of string
        /// with properties to be logged into telemetry. Including any additional properties that were passed into the method.
        /// </summary>
        /// <param name="recognizerResult">An instance of <see cref="RecognizerResult"/> to extract the telemetry properties from.</param>
        /// <param name="telemetryProperties">A collection of additional properties to be added to the returned dictionary of properties.</param>
        /// <param name="dc">An instance of <see cref="DialogContext"/>.</param>
        /// <returns>The dictionary of properties to be logged with telemetry for the recongizer result.</returns>
        protected override Dictionary<string, string> FillRecognizerResultTelemetryProperties(RecognizerResult recognizerResult, Dictionary<string, string>? telemetryProperties, DialogContext dc)
        {
            var (logPersonalInfoResult, logPersonalInfoError) = LogPersonalInformation.TryGetValue(dc.State);
            var (projectNameResult, projectNameError) = ProjectName.TryGetValue(dc.State);

            var topTwoIntents = (recognizerResult.Intents.Count > 0) ? recognizerResult.Intents.OrderByDescending(x => x.Value.Score).Take(2).ToArray() : null;

            // Add the intent score and conversation id properties
            var properties = new Dictionary<string, string>()
            {
                { CluConstants.Telemetry.ProjectNameProperty, projectNameResult },
                { CluConstants.Telemetry.IntentProperty, topTwoIntents?[0].Key ?? string.Empty },
                { CluConstants.Telemetry.IntentScoreProperty, topTwoIntents?[0].Value.Score?.ToString("N2", CultureInfo.InvariantCulture) ?? "0.00" },
                { CluConstants.Telemetry.Intent2Property, (topTwoIntents?.Length > 1) ? topTwoIntents?[1].Key ?? string.Empty : string.Empty },
                { CluConstants.Telemetry.IntentScore2Property, (topTwoIntents?.Length > 1) ? topTwoIntents?[1].Value.Score?.ToString("N2", CultureInfo.InvariantCulture) ?? "0.00" : "0.00" },
                { CluConstants.Telemetry.FromIdProperty, dc.Context.Activity.From.Id },
            };

            var entities = recognizerResult.Entities?.ToString();

            if (!string.IsNullOrWhiteSpace(entities))
            {
                properties.Add(CluConstants.Telemetry.EntitiesProperty, entities!);
            }

            // Use the LogPersonalInformation flag to toggle logging PII data, text is a common example
            if (logPersonalInfoResult && !string.IsNullOrEmpty(dc.Context.Activity.Text))
            {
                properties.Add(CluConstants.Telemetry.QuestionProperty, dc.Context.Activity.Text);
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
    }
}
