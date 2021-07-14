// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using Microsoft.Bot.Connector;
using Microsoft.Extensions.Logging;
using TranscriptTestRunner.TestClients;

namespace TranscriptTestRunner
{
    /// <summary>
    /// Factory class to create instances of <see cref="TestClientBase"/>.
    /// </summary>
    public class TestClientFactory
    {
        private readonly TestClientBase _testClientBase;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestClientFactory"/> class.
        /// </summary>
        /// <param name="channel">The type of channel to create based on the <see cref="Channels"/> class.</param>
        /// <param name="options">The options to create the client.</param>
        /// <param name="logger">An optional <see cref="ILogger"/> instance.</param>
        public TestClientFactory(string channel, DirectLineTestClientOptions options, ILogger logger)
        {
            switch (channel)
            {
                case Channels.Directline:
                    _testClientBase = new DirectLineTestClient(options, logger);
                    break;
                case Channels.Emulator:
                    break;
                case Channels.Msteams:
                    break;
                case Channels.Facebook:
                    break;
                case Channels.Slack:
                    break;
                default:
                    throw new InvalidEnumArgumentException($"Invalid client type ({channel})");
            }
        }

        /// <summary>
        /// Gets the test client.
        /// </summary>
        /// <returns>The test client.</returns>
        public TestClientBase GetTestClient()
        {
            return _testClientBase;
        }
    }
}
