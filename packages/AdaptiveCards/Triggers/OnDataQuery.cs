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
    /// Actions triggered when an invoke activity is received for a cards Action.Execute action.
    /// </summary>
    public class OnDataQuery : OnActivity
    {
        /// <summary>
        /// Class identifier.
        /// </summary>
        [JsonProperty("$kind")]
        public new const string Kind = "Microsoft.Bot.Components.OnDataQuery";

        /// <summary>
        /// Initializes a new instance of the <see cref="OnDataQuery"/> class.
        /// </summary>
        /// <param name="dataset">Optional, dataset to trigger on.</param>
        /// <param name="actions">Optional, actions to add to the plan when the rule constraints are met.</param>
        /// <param name="condition">Optional, condition which needs to be met for the actions to be executed.</param>
        /// <param name="callerPath">Optional, source file full path.</param>
        /// <param name="callerLine">Optional, line number in source file.</param>
        [JsonConstructor]
        public OnDataQuery(string dataset = null, List<Dialog> actions = null, string condition = null, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(type: ActivityTypes.Invoke, actions: actions, condition: condition, callerPath: callerPath, callerLine: callerLine)
        {
            Dataset = dataset ?? null;
        }

        /// <summary>
        /// Gets or sets dataset to trigger on.
        /// </summary>
        /// <value>
        /// Dataset to trigger on.
        /// </value>
        [JsonProperty("dataset")]
        public string Dataset { get; set; }

        /// <summary>
        /// Gets the identity for this rule's action.
        /// </summary>
        /// <returns>String with the identity.</returns>
        public override string GetIdentity()
        {
            return $"{this.GetType().Name}({this.Dataset})";
        }

        /// <inheritdoc/>
        protected override Expression CreateExpression()
        {
            // Filter to only invoked Action.Execute activities
            var expression = Expression.AndExpression(
                base.CreateExpression(),
                Expression.Parse($"{TurnPath.Activity}.name == 'adaptiveCard/action'"),
                Expression.Parse($"{TurnPath.Activity}.value.action.type == 'Data.Query'"));

            // Add optional verb constraint
            if (!string.IsNullOrEmpty(this.Dataset))
            {
                expression = Expression.AndExpression(expression, Expression.Parse($"{TurnPath.Activity}.value.action.dataset == '{this.Dataset.Trim()}'"));
            }

            return expression;
        }
    }
}
