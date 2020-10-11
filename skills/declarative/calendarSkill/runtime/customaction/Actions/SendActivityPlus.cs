using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;
using Microsoft.Bot.Schema;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotFramework.Composer.CustomAction.Actions
{
    public class SendActivityPlus : Dialog
    {
        [JsonConstructor]
        public SendActivityPlus([CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
            : base()
        {
            // enable instances of this command as debug break point
            this.RegisterSourceLocation(sourceFilePath, sourceLineNumber);
        }

        [JsonProperty("$kind")]
        public const string Kind = "Microsoft.Bot.SendActivityPlus";

        /// <summary>
        /// Gets or sets an optional expression which if is true will disable this action.
        /// </summary>
        /// <example>
        /// "user.age > 18".
        /// </example>
        /// <value>
        /// A boolean expression. 
        /// </value>
        [JsonProperty("disabled")]
        public BoolExpression Disabled { get; set; }

        [JsonConverter(typeof(StringEnumConverter), /*camelCase*/ true)]
        public enum SendOperationType
        {
            /// <summary>
            /// Sends a new activity.
            /// </summary>
            Send,

            /// <summary>
            /// Updates an existing activity in place or sends a new one.
            /// </summary>
            Update,

            /// <summary>
            /// Replaces and existing activity by first deleting the old one and then sending a new one.
            /// </summary>
            Replace,

            /// <summary>
            /// Deletes an existing activity.
            /// </summary>
            Delete,

            /// <summary>
            /// Whispers an activity to another user.
            /// </summary>
            Whisper
        }

        /// <summary>
        /// Gets or sets type of change being applied.
        /// </summary>
        /// <value>
        /// Type of change being applied.
        /// </value>
        [JsonProperty("operationType")]
        public EnumExpression<SendOperationType> OperationType { get; set; } = new EnumExpression<SendOperationType>(SendOperationType.Send);

        /// <summary>
        /// Gets or sets template for the activity.
        /// </summary>
        /// <value>
        /// Template for the activity.
        /// </value>
        [JsonProperty("activity")]
        public ITemplate<Activity> Activity { get; set; }

        [JsonProperty("activityProperty")]
        public StringExpression ActivityProperty { get; set; }

        [JsonProperty("whisperToProperty")]
        public StringExpression WhisperToProperty { get; set; } = "turn.activity.from";

        /// <summary>
        /// Gets or sets the property path for where the ID of the activity that was sent should be stored.
        /// </summary>
        /// <value>
        /// Optional property path for where the ID of the activity that was sent should be stored.
        /// </value>
        [JsonProperty("activityIdProperty")]
        public StringExpression ActivityIdProperty { get; set; } = "turn.activity.replyToId";

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


            var activityProperty = this.ActivityProperty != null ? this.ActivityProperty.GetValue(dc.State) : null;
            var operationType = OperationType.GetValue(dc.State);
            var idProperty = ActivityIdProperty != null ? ActivityIdProperty.GetValue(dc.State) : String.Empty;
            var lastId = !String.IsNullOrEmpty(idProperty) ? dc.State.GetValue<string>(idProperty) : String.Empty;

            // Get activity
            Activity activity = null;
            if (!String.IsNullOrEmpty(activityProperty))
            {
                activity = dc.State.GetValue<Activity>(activityProperty);
            }
            else if (this.Activity != null)
            {
                activity = await Activity.BindAsync(dc, dc.State).ConfigureAwait(false);
            }
            else
            {
                if (operationType != SendOperationType.Delete)
                {
                    throw new Exception($"{this.Id}: activity can only be empty for delete operations.");
                }
                else
                {
                    activity = MessageFactory.Text(null);
                }
            }

            // Check for invoke activity
            ResourceResponse response = null;
            if (dc.Context.Activity.Type == ActivityTypes.Invoke)
            {
                // Generate invoke response
                var invokeResponse = new InvokeResponse() { Status = (int)HttpStatusCode.OK, Body = activity.Value };
                activity = new Activity() { Type = ActivityTypesEx.InvokeResponse, Value = invokeResponse };
            }

            // Perform operation
            switch (operationType)
            {
                case SendOperationType.Send:
                    response = await dc.Context.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);
                    break;
                case SendOperationType.Update:
                    if (!String.IsNullOrEmpty(lastId))
                    {
                        activity.Id = lastId;
                        activity.DeliveryMode = "update";
                        await dc.Context.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        response = await dc.Context.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);
                    }
                    break;
                case SendOperationType.Replace:
                    if (!String.IsNullOrEmpty(lastId))
                    {
                        activity.DeliveryMode = "replace";
                        activity.Id = lastId;
                    }
                    response = await dc.Context.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);
                    break;
                case SendOperationType.Delete:
                    if (!String.IsNullOrEmpty(lastId))
                    {
                        activity.Id = lastId;
                        activity.DeliveryMode = "delete";
                        await dc.Context.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);
                    }
                    break;
                case SendOperationType.Whisper:
                    var recipientProperty = WhisperToProperty.GetValue(dc.State);
                    var recipient = dc.State.GetValue<JObject>(recipientProperty).ToObject<ChannelAccount>();
                    var from = dc.Context.Activity.From;
                    dc.Context.Activity.From = recipient;
                    activity.DeliveryMode = "whisper";
                    response = await dc.Context.SendActivityAsync(activity, cancellationToken).ConfigureAwait(false);
                    dc.Context.Activity.From = from;
                    break;
            }

            if (response != null && !String.IsNullOrEmpty(idProperty))
            {
                // Copy new ID to memory
                dc.State.SetValue(idProperty, response.Id);
            }

            // End Dialog
            return await dc.EndDialogAsync(response, cancellationToken).ConfigureAwait(false); ;
        }

        protected override string OnComputeId()
        {
            if (Activity is ActivityTemplate at)
            {
                return $"{this.GetType().Name}({StringUtils.Ellipsis(at.Template.Trim(), 30)})";
            }

            return $"{this.GetType().Name}('{StringUtils.Ellipsis(Activity?.ToString().Trim(), 30)}')";
        }
    }
}