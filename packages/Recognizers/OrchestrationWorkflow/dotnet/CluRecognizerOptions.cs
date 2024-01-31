using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.TraceExtensions;
using Microsoft.Bot.Components.Recognizers.Models;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.NumberWithUnit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Components.Recognizers.CLURecognizer
{
    /// <summary>
    /// Options for <see cref="CluRecognizerOptions"/>.
    /// </summary>
    public class CluRecognizerOptions : CluRecognizerOptionsBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CluRecognizerOptions"/> class.
        /// </summary>
        /// <param name="application">The CLU application to use to recognize text.</param>
        public CluRecognizerOptions(CluApplication application)
            : base(application)
        {
        }

        internal override async Task<RecognizerResult> RecognizeInternalAsync(DialogContext dialogContext, Activity activity, HttpClient httpClient, CancellationToken cancellationToken)
        {
            return await RecognizeAsync(dialogContext.Context, activity?.Text!, httpClient, cancellationToken).ConfigureAwait(false);
        }

        internal override async Task<RecognizerResult> RecognizeInternalAsync(ITurnContext turnContext, HttpClient httpClient, CancellationToken cancellationToken)
        {
            return await RecognizeAsync(turnContext, turnContext?.Activity?.AsMessageActivity()?.Text!, httpClient, cancellationToken).ConfigureAwait(false);
        }

        internal override async Task<RecognizerResult> RecognizeInternalAsync(string utterance, HttpClient httpClient, CancellationToken cancellationToken)
        {
            return await RecognizeAsync(utterance, httpClient, cancellationToken).ConfigureAwait(false);
        }

        private async Task<RecognizerResult> RecognizeAsync(ITurnContext turnContext, string utterance, HttpClient httpClient, CancellationToken cancellationToken)
        {
            RecognizerResult recognizerResult;
            JObject? cluResponse = null;

            if (string.IsNullOrWhiteSpace(utterance))
            {
                recognizerResult = new RecognizerResult
                {
                    Text = utterance,
                };
            }
            else
            {
                cluResponse = await GetCluResponseAsync(utterance, httpClient, cancellationToken).ConfigureAwait(false);
                recognizerResult = BuildRecognizerResultFromOrchestrationResponse(cluResponse, utterance);
            }

            var traceInfo = JObject.FromObject(
                new
                {
                    recognizerResult,
                    cluModel = new
                    {
                        Application.ProjectName,
                    },
                    cluResult = cluResponse,
                });

            await turnContext.TraceActivityAsync(CluConstants.TraceOptions.ActivityName, traceInfo, CluConstants.TraceOptions.TraceType, CluConstants.TraceOptions.TraceLabel, cancellationToken).ConfigureAwait(false);

            return recognizerResult;
        }

        private async Task<RecognizerResult> RecognizeAsync(string utterance, HttpClient httpClient, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(utterance))
            {
                return new RecognizerResult
                {
                    Text = utterance,
                };
            }
            else
            {
                var cluResponse = await GetCluResponseAsync(utterance, httpClient, cancellationToken).ConfigureAwait(false);

                return BuildRecognizerResultFromOrchestrationResponse(cluResponse, utterance);
            }
        }

        private async Task<JObject> GetCluResponseAsync(string utterance, HttpClient httpClient, CancellationToken cancellationToken)
        {
            var uri = BuildUri();
            var content = BuildRequestBody(utterance);

            using var request = new HttpRequestMessage(HttpMethod.Post, uri.Uri);
            using var stringContent = new StringContent(content.ToString(), Encoding.UTF8, "application/json");
            request.Content = stringContent;
            request.Headers.Add(CluConstants.RequestOptions.SubscriptionKeyHeaderName, Application.EndpointKey);

            var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return (JObject)JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync().ConfigureAwait(false), new JsonSerializerSettings { MaxDepth = null }) !;
        }

        private JObject BuildRequestBody(string utterance)
        {
            return JObject.FromObject(new
            {
                kind = CluConstants.RequestOptions.Kind,
                analysisInput = new
                {
                    conversationItem = new
                    {
                        id = CluConstants.RequestOptions.ConversationItemId,
                        participantId = CluConstants.RequestOptions.ConversationItemParticipantId,
                        text = utterance,
                    },
                },
                parameters = new
                {
                    projectName = Application.ProjectName,
                    deploymentName = Application.DeploymentName,
                    stringIndexType = CluRequestBodyStringIndexType,
                },
            });
        }

        private RecognizerResult BuildRecognizerResultFromOrchestrationResponse(JObject cluResponse, string utterance)
        {
            var result = (JObject)cluResponse[CluConstants.ResponseOptions.ResultKey] !;
            var prediction = (JObject)result[CluConstants.ResponseOptions.PredictionKey] !;
            var topIntentResultKey = (string)prediction[CluConstants.ResponseOptions.TopIntentKey] !;
            var topIntent = ObjectPath.GetPathValue<JObject>(prediction, $"intents.{topIntentResultKey}");
            var topIntentProjectKind = (string)topIntent["targetProjectKind"] !;

            switch (topIntentProjectKind)
            {
                case "Conversation":
                    return BuildRecognizerResultFromClu(topIntent, utterance);
                case "QuestionAnswering":
                    return BuildRecognizerResultFromQuestionAnswering(topIntent, utterance);
            }

            return new RecognizerResult()
            {
                Text = utterance,
                Intents = new Dictionary<string, IntentScore>()
                {
                    { "None", new IntentScore { Score = 1.0f } }
                }
            };
        }

        private RecognizerResult BuildRecognizerResultFromClu(JObject cluResponse, string utterance)
        {
            var result = (JObject)cluResponse[CluConstants.ResponseOptions.ResultKey] !;
            var prediction = (JObject)result[CluConstants.ResponseOptions.PredictionKey] !;
            var recognizerResult = new RecognizerResult
            {
                Text = utterance,
                AlteredText = utterance,
                Intents = prediction.ExtractIntents(),
                Entities = prediction.ExtractEntities(),
            };

            if (IncludeAPIResults)
            {
                recognizerResult.Properties.Add(CluConstants.RecognizerResultResponsePropertyName, cluResponse);
            }

            return recognizerResult;
        }

        private RecognizerResult BuildRecognizerResultFromQuestionAnswering(JObject cluResponse, string utterance)
        {
            var recognizerResult = new RecognizerResult
            {
                Text = utterance,
                Intents = new Dictionary<string, IntentScore>(),
            };

            if (string.IsNullOrEmpty(utterance))
            {
                recognizerResult.Intents.Add("None", new IntentScore());
                return recognizerResult;
            }

            var result = (JObject)cluResponse[QuestionAnsweringConstants.ResponseOptions.ResultKey] !;
            KnowledgeBaseAnswer? topAnswer = null;
            KnowledgeBaseAnswers? resultAnswers = null;
            if (result != null)
            {
                resultAnswers = JsonConvert.DeserializeObject<KnowledgeBaseAnswers>(result.ToString(), new JsonSerializerSettings { MaxDepth = null });
               
                if (resultAnswers != null)
                {
                    foreach (var answer in resultAnswers.Answers)
                    {
                        if (topAnswer == null || topAnswer.ConfidenceScore < answer.ConfidenceScore)
                        {
                            topAnswer = answer;
                        }
                    }
                }
            }

            if (topAnswer != null)
            {
                recognizerResult.Intents.Add(QuestionAnsweringConstants.QnAMatchIntent, new IntentScore { Score = topAnswer.ConfidenceScore });

                var answerArray = new JArray
                {
                    topAnswer.Answer
                };
                ObjectPath.SetPathValue(recognizerResult, "entities.answer", answerArray);

                var instance = new JArray();
                var data = JObject.FromObject(topAnswer);
                data["startIndex"] = 0;
                data["endIndex"] = utterance.Length;
                instance.Add(data);
                ObjectPath.SetPathValue(recognizerResult, "entities.$instance.answer", instance);

                recognizerResult.Properties["answers"] = resultAnswers?.Answers;
            }
            else
            {
                recognizerResult.Intents.Add("None", new IntentScore { Score = 1.0f });
            }

            return recognizerResult;
        }

        private UriBuilder BuildUri()
        {
            var path = new StringBuilder(Application.Endpoint);

            path.Append($"/language/:analyze-conversations?api-version={CluApiVersion}");

            var uri = new UriBuilder(path.ToString());

            var query = HttpUtility.ParseQueryString(uri.Query);

            uri.Query = query.ToString();

            return uri;
        }
    }
}
