// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net;

namespace TranscriptTestRunner.Authentication
{
    /// <summary>
    /// Session information definition.
    /// </summary>
    public class SessionInfo
    {
        /// <summary>
        /// Gets or sets the session ID.
        /// </summary>
        /// <value>
        /// The session ID.
        /// </value>
        public string SessionId { get; set; }

        /// <summary>
        /// Gets or sets the session cookie.
        /// </summary>
        /// <value>
        /// The session cookie.
        /// </value>
        public Cookie Cookie { get; set; }
    }
}
