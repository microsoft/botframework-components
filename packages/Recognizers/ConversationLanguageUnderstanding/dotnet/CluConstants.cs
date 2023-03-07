using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Bot.Components.Recognizers.CLURecognizer
{
    /// <summary>
    /// The CLU Constants.
    /// </summary>
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Constants.")]
    public static class CluConstants
    {
        /// <summary>
        /// The recognizer result response property name to include the CLU result.
        /// </summary>
        public const string RecognizerResultResponsePropertyName = "cluResult";

        /// <summary>
        /// The CLU track event constants.
        /// </summary>
        public static class TrackEventOptions
        {
            /// <summary>
            /// The name of the recognizer result event to track.
            /// </summary>
            public const string RecognizerResultEventName = "CluResult";

            /// <summary>
            /// The name of the clu result cached event to track.
            /// </summary>
            public const string ResultCachedEventName = "CluResultCached";

            /// <summary>
            /// The name of the read from cached clu result event to track.
            /// </summary>
            public const string ReadFromCachedResultEventName = "ReadFromCachedCluResult";
        }

        /// <summary>
        /// The CLU response constants.
        /// </summary>
        public static class ResponseOptions
        {
            /// <summary>
            /// The CLU response result key.
            /// </summary>
            public const string ResultKey = "result";

            /// <summary>
            /// The CLU response prediction key.
            /// </summary>
            public const string PredictionKey = "prediction";
        }

        /// <summary>
        /// The CLU trace constants.
        /// </summary>
        public static class TraceOptions
        {
            /// <summary>
            /// The name of the CLU trace activity.
            /// </summary>
            public const string ActivityName = "CluRecognizer";

            /// <summary>
            /// The value type for a CLU trace activity.
            /// </summary>
            public const string TraceType = "https://www.clu.ai/schemas/trace";

            /// <summary>
            /// The context label for a CLU trace activity.
            /// </summary>
            public const string TraceLabel = "Clu Trace";
        }

        /// <summary>
        /// The CLU HttpClient constants.
        /// </summary>
        public static class HttpClientOptions
        {
            /// <summary>
            /// The default the logical name of the HttpClient to create.
            /// </summary>
            public const string DefaultLogicalName = "clu";

            /// <summary>
            /// The default time in milliseconds to wait before the request times out.
            /// </summary>
            public const double Timeout = 100000;
        }

        /// <summary>
        /// The IBotTelemetryClient event and property names that are logged by default.
        /// </summary>
        public static class Telemetry
        {
            /// <summary>
            /// The Key used when storing a CLU Result in a custom event within telemetry.
            /// </summary>
            public const string CluResult = "CluResult";

            /// <summary>
            /// The Key used when storing a CLU Project Name in a custom event within telemetry.
            /// </summary>
            public const string ProjectNameProperty = "projectName";

            /// <summary>
            /// The Key used when storing a CLU intent in a custom event within telemetry.
            /// </summary>
            public const string IntentProperty = "intent";

            /// <summary>
            /// The Key used when storing a CLU intent score in a custom event within telemetry.
            /// </summary>
            public const string IntentScoreProperty = "intentScore";

            /// <summary>
            /// The Key used when storing a CLU intent in a custom event within telemetry.
            /// </summary>
            public const string Intent2Property = "intent2";

            /// <summary>
            /// The Key used when storing a CLU intent score in a custom event within telemetry.
            /// </summary>
            public const string IntentScore2Property = "intentScore2";

            /// <summary>
            /// The Key used when storing CLU entities in a custom event within telemetry.
            /// </summary>
            public const string EntitiesProperty = "entities";

            /// <summary>
            /// The Key used when storing the CLU query in a custom event within telemetry.
            /// </summary>
            public const string QuestionProperty = "question";

            /// <summary>
            /// The Key used when storing the FromId in a custom event within telemetry.
            /// </summary>
            public const string FromIdProperty = "fromId";
        }

        /// <summary>
        /// The CLU request body default constants.
        /// </summary>
        public static class RequestOptions
        {
            /// <summary>
            /// The Kind value of the CLU request body.
            /// </summary>
            public const string Kind = "Conversation";

            /// <summary>
            /// The Conversation Item Id value of the CLU request body.
            /// </summary>
            public const string ConversationItemId = "1";

            /// <summary>
            /// The Conversation Item Participant Id value of the CLU request body.
            /// </summary>
            public const string ConversationItemParticipantId = "1";

            /// <summary>
            /// The String Index Type value of the CLU request body.
            /// </summary>
            public const string StringIndexType = "TextElement_V8";

            /// <summary>
            /// The API Version of the CLU service.
            /// </summary>
            public const string ApiVersion = "2022-05-01";

            /// <summary>
            /// The name of the CLU subscription key header.
            /// </summary>
            public const string SubscriptionKeyHeaderName = "Ocp-Apim-Subscription-Key";
        }
    }
}
