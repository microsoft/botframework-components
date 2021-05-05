// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AdaptiveExpressions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Components.AdaptiveCards
{
    /// <summary>
    /// Actions triggered when a card is submitted as a message activity via Action.Submit.
    /// </summary>
    public class OnActionSubmit : OnActivity
    {
        /// <summary>
        /// Class identifier.
        /// </summary>
        [JsonProperty("$kind")]
        public new const string Kind = "Microsoft.Bot.Components.OnActionSubmit";

        /// <summary>
        /// Initializes a new instance of the <see cref="OnActionSubmit"/> class.
        /// </summary>
        /// <param name="actions">Optional, actions to add to the plan when the rule constraints are met.</param>
        /// <param name="condition">Optional, condition which needs to be met for the actions to be executed.</param>
        /// <param name="callerPath">Optional, source file full path.</param>
        /// <param name="callerLine">Optional, line number in source file.</param>
        [JsonConstructor]
        public OnActionSubmit(List<Dialog> actions = null, string condition = null, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(type: ActivityTypes.Message, actions: actions, condition: condition, callerPath: callerPath, callerLine: callerLine)
        {
        }

        /// <inheritdoc/>
        protected override Expression CreateExpression()
        {
            // Filter to messages with empty text and a value of type "object".
            return Expression.AndExpression(
                base.CreateExpression(),
                Expression.Parse($"length({TurnPath.Activity}.text) == 0"),
                Expression.Parse($"isObject({TurnPath.Activity}.value)"));
        }
    }
}
