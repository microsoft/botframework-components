using System.Collections.Generic;
using System.Globalization;
using Microsoft.Bot.Builder;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Components.Recognizers.CLURecognizer
{
    internal static class CluExtensions
    {
        internal static IDictionary<string, IntentScore> ExtractIntents(this JObject cluResult)
        {
            var result = new Dictionary<string, IntentScore>();

            if (cluResult?["intents"] != null && cluResult["intents"] is JArray cluIntents)
            {
                foreach (var intent in cluIntents)
                {
                    var category = intent["category"] !.ToString();
                    var confidenceScore = intent["confidenceScore"] !.ToString();

                    result.Add(
                        NormalizedValue(category),
                        new IntentScore
                        {
                            Score = confidenceScore == null ? 0.0 : double.Parse(confidenceScore, CultureInfo.InvariantCulture),
                        });
                }
            }

            return result;
        }

        internal static JObject ExtractEntities(this JObject cluResult)
        {
            var result = new JObject();

            if (cluResult?["entities"] != null && cluResult["entities"] is JArray cluEntities)
            {
                foreach (var entity in cluEntities)
                {
                    var category = entity["category"] !.ToString();

                    if (result[NormalizedValue(category)] == null)
                    {
                        result[NormalizedValue(category)] = new JArray(entity);
                        continue;
                    }

                    ((JArray)result[NormalizedValue(category)] !).Add(entity);
                }
            }

            return result;
        }

        private static string NormalizedValue(string value) => value.Replace('.', '_').Replace(' ', '_');
    }
}
