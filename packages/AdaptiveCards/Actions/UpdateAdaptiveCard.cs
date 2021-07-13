// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Components.AdaptiveCards
{
    public class UpdateAdaptiveCard : BaseAdaptiveCard
    {
        [JsonProperty("$kind")]
        public const string Kind = "Microsoft.Bot.Components.UpdateAdaptiveCard";

        [JsonProperty("activityId")]
        public StringExpression ActivityId { get; set; } = "=turn.activity.replyTo";

        protected async override Task<object> OnProcessCardAsync(DialogContext dc, JObject card, CancellationToken cancellationToken = default)
        {
            // Create activity
            var activity = CreateActivity(dc, card);
            if (activity.Type != ActivityTypesEx.InvokeResponse)
            {
                // Get activity ID
                var activityId = ActivityId?.GetValue(dc.State);
                if (string.IsNullOrEmpty(activityId))
                {
                    throw new Exception($"{this.Id}: a valid '{nameof(ActivityId)}' wasn't provided.");
                }

                // Update existing activity
                await dc.Context.UpdateActivityAsync(activity, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // Send invoke response
                await dc.Context.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);
            }

            return null;
        }
    }
}
