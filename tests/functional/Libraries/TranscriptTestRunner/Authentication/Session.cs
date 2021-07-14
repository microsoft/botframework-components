// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Newtonsoft.Json;

namespace TranscriptTestRunner.Authentication
{
    /// <summary>
    /// Session definition.
    /// </summary>
    public class Session
    {
        /// <summary>
        /// Gets or sets the session ID.
        /// </summary>
        /// <value>
        /// The session ID.
        /// </value>
        [JsonProperty("sessionId")]
        public string SessionId { get; set; }
    }
}
