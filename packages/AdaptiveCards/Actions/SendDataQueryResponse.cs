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
    public class SendDataQueryResponse : Dialog
    {
        [JsonProperty("$kind")]
        public const string Kind = "Microsoft.Bot.Components.SendDataQueryResponse";

        [JsonConstructor]
        public SendDataQueryResponse([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            : base()
        {
            // enable instances of this command as debug break point
            this.RegisterSourceLocation(sourceFilePath, sourceLineNumber);
        }

        [JsonProperty("disabled")]
        public BoolExpression Disabled { get; set; }

        [JsonProperty("results")]
        public ArrayExpression<object> Results { get; set; }

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

            if (dc.Context.Activity.Type != ActivityTypes.Invoke || dc.Context.Activity.Name != "adaptiveCard/action")
            {
                throw new Exception($"{this.Id}: should only be called in repsonse to 'invoke' activities with a name of 'adaptiveCard/action'.");
            }

            // Get results
            var results = Results?.GetValue(dc.State);

            // Send invoke response
            var activity = new Activity(type: ActivityTypesEx.InvokeResponse, value: new InvokeResponse() { Status = 200, Body = results });
            await dc.Context.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);

            return await dc.EndDialogAsync(null, cancellationToken).ConfigureAwait(false);
        }

        protected override string OnComputeId()
        {
            return $"{this.GetType().Name}('{StringUtils.Ellipsis(Results?.ToString(), 30)}')";
        }
    }
}
