﻿// Licensed under the MIT License.
// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AdaptiveExpressions;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;

namespace Microsoft.Bot.Components.Teams.Conditions
{
    /// <summary>
    /// Actions triggered when a Teams InvokeActivity is received when activity.name='composeExtension/fetchTask'.
    /// </summary>
    public class OnTeamsMEFetchTask : OnInvokeActivity
    {
        [JsonProperty("$kind")]
        public new const string Kind = "Teams.OnMEFetchTask";

        [JsonProperty("commandId")]
        public string CommandId { get; set; }

        [JsonConstructor]
        public OnTeamsMEFetchTask(string commandId = null, List<Dialog> actions = null, string condition = null, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(actions: actions, condition: condition, callerPath: callerPath, callerLine: callerLine)
        {
            CommandId = commandId;
        }

        /// <inheritdoc/>
        protected override Expression CreateExpression()
        {
            // if name is 'composeExtension/fetchTask'
            var expressions = new List<Expression>
            {
                Expression.Parse($"{TurnPath.Activity}.ChannelId == '{Channels.Msteams}' && {TurnPath.Activity}.name == 'composeExtension/fetchTask'"),
                base.CreateExpression()
            };

            if (!string.IsNullOrEmpty(CommandId))
            {
                expressions.Add(Expression.Parse($"{TurnPath.Activity}.value.commandId == '{CommandId}'"));
            }

            return Expression.AndExpression(expressions.ToArray());
        }
    }
}
