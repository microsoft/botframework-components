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
using Newtonsoft.Json.Linq;

namespace Microsoft.Bot.Components.AdaptiveCards
{
    public abstract class BaseAdaptiveCard : Dialog
    {
        protected abstract Task<object> OnProcessCardAsync(DialogContext dc, JObject card, CancellationToken cancellationToken = default(CancellationToken));

        [JsonConstructor]
        public BaseAdaptiveCard([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            : base()
        {
            // enable instances of this command as debug break point
            this.RegisterSourceLocation(sourceFilePath, sourceLineNumber);
        }

        [JsonProperty("disabled")]
        public BoolExpression Disabled { get; set; }

        [JsonProperty("template")]
        public ObjectExpression<object> Template { get; set; } = new ObjectExpression<object>();

        [JsonProperty("data")]
        public ObjectExpression<object> Data { get; set; } = new ObjectExpression<object>("={}");

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

            // Get template
            var jTemplate = (JObject)this.Template?.GetValue(dc.State);
            if (jTemplate == null)
            {
                throw new Exception($"{this.Id}: a template was not provided or is not valid JSON.");
            }

            // Get data
            var data = this.Data?.GetValue(dc.State) ?? new JObject();

            // Render card and convert to JObject
            var tmpl = new AdaptiveCardTemplate(jTemplate.ToString());
            var card = tmpl.Expand(data);
            var jCard = !String.IsNullOrEmpty(card) ? JObject.Parse(card) : null;

            // Process card
            var result = await OnProcessCardAsync(dc, jCard, cancellationToken).ConfigureAwait(false);

            return await dc.EndDialogAsync(result, cancellationToken).ConfigureAwait(false);
        }

        protected Activity CreateActivity(DialogContext dc, JObject card)
        {
            Activity activity;
            if (dc.Context.Activity.Type == ActivityTypes.Invoke)
            {
                // Ensure we're responding to a supported invoke name
                if (dc.Context.Activity.Name != "adaptiveCard/action")
                {
                    throw new Exception($"{this.Id}: doesn't support responding to an invoke named '{dc.Context.Activity.Name}'.");
                }

                // Create invoke response
                var response = new JObject()
                {
                    { "statusCode", 200 },
                    { "type", "application/vnd.microsoft.card.adaptive" },
                    { "value", card }
                };

                activity = new Activity(type: ActivityTypesEx.InvokeResponse, value: new InvokeResponse() { Status = 200, Body = response });
            }
            else
            {
                // Create message activity
                activity = (Activity)MessageFactory.Attachment(new Attachment(contentType: "application/vnd.microsoft.card.adaptive", content: card));
            }

            return activity;
        }

        protected override string OnComputeId()
        {
            return $"{this.GetType().Name}('{StringUtils.Ellipsis(Data?.ToString() + Template?.ToString(), 30)}')";
        }
    }
}
