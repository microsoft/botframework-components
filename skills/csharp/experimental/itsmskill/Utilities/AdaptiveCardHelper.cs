// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using AdaptiveCards;
using Newtonsoft.Json;

namespace ITSMSkill.Utilities
{
    public class AdaptiveCardHelper
    {
        // TODO: Replace with Cards.Lg
        public static AdaptiveCard GetCardFromJson(string jsonFile)
        {
            string jsonCard = GetJson(jsonFile);

            return JsonConvert.DeserializeObject<AdaptiveCard>(jsonCard);
        }

        private static string GetJson(string jsonFile)
        {
            var dir = Path.GetDirectoryName(typeof(AdaptiveCardHelper).Assembly.Location);
            var filePath = Path.Combine(dir, $"{jsonFile}");
            return File.ReadAllText(filePath);
        }
    }
}
