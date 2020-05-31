// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace Microsoft.Bot.Solutions.Extensions.Utilities
{
    public class UserReference
    {
        [JsonConstructor]
        public UserReference()
        {
        }

        public UserReference(ITurnContext turnContext)
        {
            Update(turnContext);
        }

        [JsonIgnore]
        public IDictionary<string, CancellationTokenSource> CTSs { get; set; } = new Dictionary<string, CancellationTokenSource>();

        public ConversationReference Reference { get; set; }

        public bool Cancel(string id)
        {
            if (!string.IsNullOrEmpty(id) && CTSs.ContainsKey(id))
            {
                bool hasCancelled = true;
                var cts = CTSs[id];
                if (!cts.IsCancellationRequested)
                {
                    hasCancelled = false;
                    cts.Cancel();
                }

                CTSs.Remove(id);
                return !hasCancelled;
            }

            return false;
        }

        public void Update(ITurnContext turnContext)
        {
            Reference = turnContext.Activity.GetConversationReference();
        }
    }
}
