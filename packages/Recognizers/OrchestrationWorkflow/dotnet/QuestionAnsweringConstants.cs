using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Bot.Components.Recognizers.CLURecognizer
{
    /// <summary>
    /// The CLU Constants.
    /// </summary>
    [SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "Constants.")]
    public static class QuestionAnsweringConstants
    {
        public const string QnAMatchIntent = "QnAMatch";

        public static class ResponseOptions
        {
            /// <summary>
            /// The Question Answering response result key.
            /// </summary>
            public const string ResultKey = "result";
        }
    }
}
