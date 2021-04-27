﻿// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AdaptiveExpressions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Newtonsoft.Json;

namespace Microsoft.Bot.Components.Teams.Conditions
{
    /// <summary>
    /// Actions triggered when a Teams InvokeActivity is received with activity.name == 'composeExtension/queryLink'.
    /// </summary>
    public class OnTeamsAppBasedLinkQuery : OnInvokeActivity
    {
        [JsonConstructor]
        public OnTeamsAppBasedLinkQuery(List<Dialog> actions = null, string condition = null, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(actions: actions, condition: condition, callerPath: callerPath, callerLine: callerLine)
        {
        }

        [JsonProperty("$kind")]
        public new const string Kind = "Teams.OnAppBasedLinkQuery";

        /// <inheritdoc/>
        protected override Expression CreateExpression()
        {
            // if name is 'composeExtension/queryLink'
            return Expression.AndExpression(Expression.Parse($"{TurnPath.Activity}.name == 'composeExtension/queryLink'"), base.CreateExpression());
        }
    }
}
