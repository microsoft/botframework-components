using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Options;

namespace Microsoft.Bot.Components.Recognizers.CLURecognizer
{
    /// <summary>
    /// CLU Recognizer Options.
    /// </summary>
    public abstract class CluRecognizerOptionsBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CluRecognizerOptionsBase"/> class.
        /// </summary>
        /// <param name="application">An instance of <see cref="CluApplication"/>.</param>
        protected CluRecognizerOptionsBase(CluApplication application)
        {
            Application = application ?? throw new ArgumentNullException(nameof(application));
        }

        /// <summary>
        /// Gets the CLU application used to recognize text.
        /// </summary>
        /// <value>
        /// The CLU application to use to recognize text.
        /// </value>
        public CluApplication Application { get; private set; }

        /// <summary>
        /// Gets or sets the time in milliseconds to wait before the request times out.
        /// </summary>
        /// <value>
        /// The time in milliseconds to wait before the request times out. Default is 100000 milliseconds.
        /// </value>
        public double Timeout { get; set; } = CluConstants.HttpClientOptions.Timeout;

        /// <summary>
        /// Gets or sets the IBotTelemetryClient used to log the CluResult event.
        /// </summary>
        /// <value>
        /// The client used to log telemetry events.
        /// </value>
        public IBotTelemetryClient TelemetryClient { get; set; } = new NullBotTelemetryClient();

        /// <summary>
        /// Gets or sets a value indicating whether to log personal information that came from the user to telemetry.
        /// </summary>
        /// <value>If true, personal information is logged to Telemetry; otherwise the properties will be filtered.</value>
        public bool LogPersonalInformation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether flag to indicate if full results from the CLU API should be returned with the recognizer result.
        /// </summary>
        /// <value>A value indicating whether full results from the CLU API should be returned with the recognizer result.</value>
        public bool IncludeAPIResults { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the string index type to include in the the CLU request body.
        /// </summary>
        /// <value>A value indicating the string index type to include in the the CLU request body.</value>
        public string CluRequestBodyStringIndexType { get; set; } = CluConstants.RequestOptions.StringIndexType;

        /// <summary>
        /// Gets or sets a value indicating the api version of the CLU service.
        /// </summary>
        /// <value>A value indicating the api version of the CLU service.</value>
        public string CluApiVersion { get; set; } = CluConstants.RequestOptions.ApiVersion;

        /// <summary>
        /// Gets or sets a value indicating the logical name of the client to create.
        /// </summary>
        /// <value>A value indicating the logical name of the client to create.</value>
        public string LogicalHttpClientName { get; set; } = Options.DefaultName;

        // Support original ITurnContext
        internal abstract Task<RecognizerResult> RecognizeInternalAsync(ITurnContext turnContext, HttpClient httpClient, CancellationToken cancellationToken);

        // Support DialogContext
        internal abstract Task<RecognizerResult> RecognizeInternalAsync(DialogContext context, Activity activity, HttpClient httpClient, CancellationToken cancellationToken);

        // Support string utterance
        internal abstract Task<RecognizerResult> RecognizeInternalAsync(string utterance, HttpClient httpClient, CancellationToken cancellationToken);
    }
}
