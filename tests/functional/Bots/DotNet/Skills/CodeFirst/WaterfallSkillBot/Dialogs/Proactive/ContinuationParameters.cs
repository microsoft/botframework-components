// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Security.Principal;
using Microsoft.Bot.Schema;

namespace Microsoft.BotFrameworkFunctionalTests.WaterfallSkillBot.Dialogs.Proactive
{
    /// <summary>
    /// Stores the information needed to resume a conversation when a proactive message arrives.
    /// </summary>
    public class ContinuationParameters
    {
        public IIdentity ClaimsIdentity { get; set; }

        public string OAuthScope { get; set; }

        public ConversationReference ConversationReference { get; set; }
    }
}
