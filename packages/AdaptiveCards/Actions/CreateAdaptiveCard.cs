// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Components.AdaptiveCards
{
    public class CreateAdaptiveCard : BaseAdaptiveCard
    {
        [JsonProperty("$kind")]
        public const string Kind = "Microsoft.Bot.Components.CreateAdaptiveCard";

        [JsonProperty("resultProperty")]
        public StringExpression ResultProperty { get; set; }

        protected override Task<object> OnProcessCardAsync(DialogContext dc, JObject card, CancellationToken cancellationToken = default)
        {
            // Write card to memory
            var resultProperty = this.ResultProperty?.GetValue(dc.State);

            if (!string.IsNullOrEmpty(resultProperty))
            {
                dc.State.SetValue(resultProperty, card);
            }

            return Task.FromResult(card as object);
        }
    }
}
