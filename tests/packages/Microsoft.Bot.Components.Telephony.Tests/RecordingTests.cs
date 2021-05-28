// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.Bot.Components.Telephony.Tests
{
    public class RecordingTests : IntegrationTestsBase
    {
        public RecordingTests(ResourceExplorerFixture resourceExplorerFixture) : base(resourceExplorerFixture)
        {
        }

        [Fact]
        public async Task StartRecording_HappyPath()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }

        [Fact]
        public async Task StartRecording_WithTangent_InterruptionEnabled()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }

        [Fact]
        public async Task StartRecording_WithTangent_InterruptionDisabled()
        {
            await TestUtils.RunTestScript(
                _resourceExplorerFixture.ResourceExplorer,
                adapterChannel: Channels.Telephony,
                configuration: new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>() { { "allowInterruptions", "false" } })
                    .Build());
        }

        [Fact]
        public async Task StartRecording_CommandResultWrongName()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }

        [Fact]
        public async Task StartRecording_IgnoredInNonTelephonyChannel()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Msteams);
        }
    }
}
