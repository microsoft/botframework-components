// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Components.Teams.Actions
{
    /// <summary>
    /// Calls TeamsInfo.SendMessageToTeamsChannel and sets the result to a memory property.
    /// </summary>
    public class SendMessageToTeamsChannel : Dialog
    {
        /// <summary>
        /// Class identifier.
        /// </summary>
        [JsonProperty("$kind")]
        public const string Kind = "Teams.SendMessageToTeamsChannel";

        /// <summary>
        /// Initializes a new instance of the <see cref="SendMessageToTeamsChannel"/> class.
        /// </summary>
        /// <param name="callerPath">Optional, source file full path.</param>
        /// <param name="callerLine">Optional, line number in source file.</param>
        [JsonConstructor]
        public SendMessageToTeamsChannel([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base()
        {
            RegisterSourceLocation(callerPath, callerLine);
        }

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

        /// <summary>
        /// Gets or sets property path to put the newly created activity's Conversation Reference.
        /// This can be used to later send messages to this same conversation.
        /// </summary>
        /// <value>
        /// Property path to put the newly created activity's conversation reference.
        /// </value>
        [JsonProperty("conversationReferenceProperty")]
        public StringExpression ConversationReferenceProperty { get; set; }

        /// <summary>
        /// Gets or sets property path to put the id of the activity sent.
        /// </summary>
        /// <value>
        /// Property path to put the id of the activity sent.
        /// </value>
        [JsonProperty("activityIdProperty")]
        public StringExpression ActivityIdProperty { get; set; }

        /// <summary>
        /// Gets or sets the expression to get the value to use for the teams channel id where the message should be sent. Default is turn.activity.channelData.channel.id.
        /// </summary>
        /// <value>
        /// The expression to get the value to use for teams channel id.
        /// </value>
        [JsonProperty("teamsChannelId")]
        public StringExpression TeamsChannelId { get; set; } = "=turn.activity.channelData.channel.id";

        /// <summary>
        /// Gets or sets template for the activity expression containing the activity to send.
        /// </summary>
        /// <value>
        /// Template for the activity.
        /// </value>
        [JsonProperty("activity")]
        public ITemplate<Activity> Activity { get; set; }

        /// <inheritdoc/>
        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (options is CancellationToken)
            {
                throw new ArgumentException($"{nameof(options)} cannot be a cancellation token");
            }
            
            if (Disabled != null && Disabled.GetValue(dc.State))
            {
                return await dc.EndDialogAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            if (dc.Context.Activity.ChannelId != Channels.Msteams)
            {
                throw new InvalidOperationException($"{Kind} works only on the Teams channel.");
            }

            IActivity activity = null;
            if (Activity != null)
            {
                activity = await Activity.BindAsync(dc, dc.State).ConfigureAwait(false);
            }

            string teamsChannelId = TeamsChannelId.GetValueOrNull(dc.State);
            if (string.IsNullOrEmpty(teamsChannelId))
            {
                teamsChannelId = dc.Context.Activity.TeamsGetChannelId();
            }

            Tuple<ConversationReference, string> result;

            if (dc.Context.Adapter is CloudAdapterBase)
            {
                // Retrieve the bot appid from TurnState's ClaimsIdentity
                string appId;
                if (dc.Context.TurnState.Get<ClaimsIdentity>(BotAdapter.BotIdentityKey) is ClaimsIdentity botIdentity)
                {
                    // Apparently 'version' is sometimes empty, which will result in no id returned from GetAppIdFromClaims
                    // TODO: Replace with new method
                    //appId = JwtTokenValidation.GetAppIdFromClaims(botIdentity.Claims);
                    appId = string.Empty;
                    if (string.IsNullOrEmpty(appId))
                    {
                        appId = botIdentity.Claims.FirstOrDefault(claim => claim.Type == AuthenticationConstants.AudienceClaim)?.Value;
                    }

                    if (string.IsNullOrEmpty(appId))
                    {
                        throw new InvalidOperationException($"Missing AppIdClaim in ClaimsIdentity.");
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Missing {BotAdapter.BotIdentityKey} in {nameof(ITurnContext)} TurnState.");
                }

                // The result comes back as a tuple, which is used to set the two properties (if present).
                result = await TeamsInfo.SendMessageToTeamsChannelAsync(dc.Context, activity, teamsChannelId, appId, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                throw new InvalidOperationException($"The adapter does not support {nameof(SendMessageToTeamsChannel)}.");
            }

            if (ConversationReferenceProperty != null)
            {
                dc.State.SetValue(ConversationReferenceProperty.GetValue(dc.State), result.Item1);
            }
            
            if (ActivityIdProperty != null)
            {
                dc.State.SetValue(ActivityIdProperty.GetValue(dc.State), result.Item2);
            }

            return await dc.EndDialogAsync(result, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        protected override string OnComputeId()
        {
            return $"{GetType().Name}[{TeamsChannelId?.ToString() ?? string.Empty},{ActivityIdProperty?.ToString() ?? string.Empty},{ConversationReferenceProperty?.ToString() ?? string.Empty}]";
        }
    }
}
