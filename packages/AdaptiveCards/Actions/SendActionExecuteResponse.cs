// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCards.Templating;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Components.AdaptiveCards
{
    [JsonConverter(typeof(StringEnumConverter), /*camelCase*/ true)]
    public enum ValueType
    {
        /// <summary>
        /// The response includes an Adaptive Card
        /// </summary>
        AdaptiveCard,

        /// <summary>
        /// The response includes a message
        /// </summary>
        Message
    }

    public class SendActionExecuteResponse : Dialog
    {
        [JsonProperty("$kind")]
        public const string Kind = "Microsoft.Bot.Components.SendActionExecuteResponse";

        [JsonConstructor]
        public SendActionExecuteResponse([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            : base()
        {
            // enable instances of this command as debug break point
            this.RegisterSourceLocation(sourceFilePath, sourceLineNumber);
        }

        [JsonProperty("disabled")]
        public BoolExpression Disabled { get; set; }

        [JsonProperty("statusCode")]
        public IntExpression StatusCode { get; set; } = new IntExpression(200);

        [JsonProperty("type")]
        public EnumExpression<ValueType> Type { get; set; } = new EnumExpression<ValueType>(ValueType.Message);

        [JsonProperty("value")]
        public ValueExpression Value { get; set; }

        public async override Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (options is CancellationToken)
            {
                throw new ArgumentException($"{nameof(options)} cannot be a cancellation token");
            }

            if (this.Disabled != null && this.Disabled.GetValue(dc.State) == true)
            {
                return await dc.EndDialogAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            if (dc.Context.Activity.Type != ActivityTypes.Invoke || !string.Equals(dc.Context.Activity.Name, BaseAdaptiveCard.AdaptiveCardActionName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"{this.Id}: should only be called in repsonse to 'invoke' activities with a name of '{BaseAdaptiveCard.AdaptiveCardActionName}'.");
            }

            // Get parameters
            var statusCode = this.StatusCode?.GetValue(dc.State);
            var type = this.Type?.GetValue(dc.State);
            var value = this.Value?.GetValue(dc.State);
            string mimeType = string.Empty;

            // Validate params
            if (statusCode != null && statusCode >= 200 && statusCode < 300)
            {
                switch (type)
                {
                    case ValueType.Message:
                        // TODO: Migrate to use attachment constants once added.
                        mimeType = "application/vnd.microsoft.activity.message";
                        if (!(value is string))
                        {
                            throw new ArgumentException($"{this.Id}: Message 'value' isn't of type String.");
                        }

                        break;
                    case ValueType.AdaptiveCard:
                        // TODO: Migrate to use attachment constants once added.
                        mimeType = "application/vnd.microsoft.card.adaptive";
                        if (!(value is JObject))
                        {
                            throw new ArgumentException($"{this.Id}: Card 'value' isn't of type Object.");
                        }

                        break;
                    default:
                        throw new ArgumentException($"{this.Id}: A 'type' wasn't specified.");
                }
            }
            
            // Send invoke response
            var response = new JObject()
            {
                { "statusCode", statusCode },
                { "type", mimeType },
                { "value", JToken.FromObject(value) }
            };
            var activity = new Activity(type: ActivityTypesEx.InvokeResponse, value: new InvokeResponse() { Status = 200, Body = response });
            await dc.Context.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);

            return await dc.EndDialogAsync(null, cancellationToken).ConfigureAwait(false);
        }

        protected override string OnComputeId()
        {
            return $"{this.GetType().Name}('{StringUtils.Ellipsis(Value?.ToString(), 30)}')";
        }
    }
}
