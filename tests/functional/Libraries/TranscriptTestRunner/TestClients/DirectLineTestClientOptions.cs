// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace TranscriptTestRunner.TestClients
{
    /// <summary>
    /// Class with the options needed to run a test against a TesClient.
    /// </summary>
    public class DirectLineTestClientOptions
    {
        /// <summary>
        /// Gets or sets the Id of the host bot.
        /// </summary>
        /// <value>The Id of the host bot.</value>
        public string BotId { get; set; }

        /// <summary>
        /// Gets or sets the secret for the connection with the test client.
        /// </summary>
        /// <value>The secret for the connection with the test client.</value>
        public string DirectLineSecret { get; set; }

        /// <summary>
        /// Gets or sets the timeout for starting a conversation in milliseconds (default is 120000).
        /// </summary>
        /// <value>
        /// The timeout for starting a conversation (in milliseconds).
        /// </value>
        public int StartConversationTimeout { get; set; } = 120000;

        /// <summary>
        /// Gets or sets the maximum number of attempts to start a conversation (default is 3).
        /// </summary>
        /// <value>
        /// The maximum number of attempts to start a conversation.
        /// </value>
        public int StartConversationMaxAttempts { get; set; } = 3;
    }
}
