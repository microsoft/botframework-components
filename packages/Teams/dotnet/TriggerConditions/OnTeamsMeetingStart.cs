// Licensed under the MIT License.
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
    /// Actions triggered when a Teams Meeting Start event is received.
    /// </summary>
    /// <remarks>
    /// turn.activity.value has meeting data.
    /// </remarks>
    public class OnTeamsMeetingStart : OnEventActivity
    {
        [JsonProperty("$kind")]
        public new const string Kind = "Teams.OnMeetingStart";

        [JsonConstructor]
        public OnTeamsMeetingStart(List<Dialog> actions = null, string condition = null, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(actions: actions, condition: condition, callerPath: callerPath, callerLine: callerLine)
        {
        }

        /// <inheritdoc/>
        protected override Expression CreateExpression()
        {
            return Expression.AndExpression(Expression.Parse($"{TurnPath.Activity}.channelId == '{Channels.Msteams}' && {TurnPath.Activity}.name == 'application/vnd.microsoft.meetingStart'"), base.CreateExpression());
        }
    }
}
