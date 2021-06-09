// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace TranscriptTestRunner.Authentication
{
    /// <summary>
    /// Token definition.
    /// </summary>
    public class TokenInfo
    {
        /// <summary>
        /// Gets or sets the token string.
        /// </summary>
        /// <value>
        /// The token string.
        /// </value>
        [JsonProperty("token")]
        public string Token { get; set; }

        /// <summary>
        /// Gets or sets the conversation ID.
        /// </summary>
        /// <value>
        /// The conversation ID.
        /// </value>
        [JsonProperty("conversationId")]
        public string ConversationId { get; set; }
    }
}
