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
    public class SendAdaptiveCard : BaseAdaptiveCard
    {
        [JsonProperty("$kind")]
        public const string Kind = "Microsoft.Bot.Components.SendAdaptiveCard";

        [JsonProperty("activityIdProperty")]
        public StringExpression ActivityIdProperty { get; set; }

        protected async override Task<object> OnProcessCardAsync(DialogContext dc, JObject card, CancellationToken cancellationToken = default)
        {
            // Send activity to client
            var activity = CreateActivity(dc, card);
            var response = await dc.Context.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);

            // Get activity ID (if there is one)
            var activityId = response != null && !string.IsNullOrEmpty(response.Id) ? response.Id : string.Empty;

            // Save actvity ID to memory
            var idProperty = ActivityIdProperty?.GetValue(dc.State);

            if (!string.IsNullOrEmpty(idProperty))
            {
                dc.State.SetValue(idProperty, activityId);
            }

            return activityId;
        }
    }
}
