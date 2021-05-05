// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AdaptiveExpressions;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions
{
    /// <summary>
    /// Actions triggered when ConversationUpdateActivity is received with Activity.MembersRemoved > 0.
    /// </summary>
    public class OnMembersAdded : OnActivity
    {
        /// <summary>
        /// Gets the unique name of this trigger.
        /// </summary>
        [JsonProperty("$kind")]
        public new const string Kind = "Microsoft.OnMembersAdded";

        /// <summary>
        /// Initializes a new instance of the <see cref="OnMembersAdded"/> class.
        /// </summary>
        [JsonConstructor]
        public OnMembersAdded(List<Dialog> actions = null, string condition = null, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(type: ActivityTypes.ConversationUpdate, actions: actions, condition: condition, callerPath: callerPath, callerLine: callerLine)
        {
        }

        /// <inheritdoc/>
        protected override Expression CreateExpression()
        {
            // The Activity.MembersAdded list must have more than 0 items.
            return Expression.AndExpression(Expression.Parse($"count({TurnPath.Activity}.MembersAdded) > 0"), base.CreateExpression());
        }
    }
}
