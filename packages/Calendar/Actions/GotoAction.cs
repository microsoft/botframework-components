﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AdaptiveExpressions.Properties;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Extensions.Actions
{
    /// <summary>
    /// This action implements the BF SDK GotoAction for Composer. 
    /// This action has not yet been exposed by the Composer team but is 
    /// supported by the SDK and enables looping through a set of actions 
    /// and going back to a specific point in a dialog.
    /// This action will be removed once it is fully implemented by Composer.
    /// </summary>
    public class GotoAction : Dialog
    {
        [JsonProperty("$kind")]
        public const string DeclarativeType = "Microsoft.Bot.Solutions.GotoAction";

        public GotoAction(string actionId = null, [CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
        {
            this.RegisterSourceLocation(callerPath, callerLine);
            this.ActionId = actionId;
        }

        /// <summary>
        /// Gets or sets the action Id to goto.
        /// </summary>
        /// <value>The action Id.</value>
        public StringExpression ActionId { get; set; }

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

        public override async Task<DialogTurnResult> BeginDialogAsync(DialogContext dc, object options = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (options is CancellationToken)
            {
                throw new ArgumentException($"{nameof(options)} cannot be a cancellation token");
            }

            if (this.Disabled != null && this.Disabled.GetValue(dc.State) == true)
            {
                return await dc.EndDialogAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            // Continue is simply returning an ActionScopeResult with GotoAction and actionId command in it.
            var actionScopeResult = new ActionScopeResult()
            {
                ActionScopeCommand = ActionScopeCommands.GotoAction,
                ActionId = this.ActionId?.GetValue(dc.State) ?? throw new ArgumentNullException(nameof(ActionId))
            };

            return await dc.EndDialogAsync(result: actionScopeResult, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
