// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Adapters.Slack;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Bot.Builder.Runtime.Samples
{
    public class RuntimeSlackAdapter : SlackAdapter
    {
        public RuntimeSlackAdapter(IConfiguration configuration, ConversationState conversationState, UserState userState)
            : base(configuration)
        {
            this.UseBotState(conversationState, userState);
        }
    }
}
