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
    public class BatchInputTests : IntegrationTestsBase
    {
        public BatchInputTests(ResourceExplorerFixture resourceExplorerFixture) : base(resourceExplorerFixture)
        {
        }

        [Fact]
        public async Task BatchInput_TerminationHappyPath()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }

        [Fact]
        public async Task BatchInput_FixedLengthHappyPath()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }

        [Fact]
        public async Task BatchInput_Termination_WithTangent_InterruptionEnabled()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }

        [Fact]
        public async Task BatchInput_Termination_WithTangent_InterruptionDisabled()
        {
            await TestUtils.RunTestScript(
                _resourceExplorerFixture.ResourceExplorer,
                adapterChannel: Channels.Telephony,
                configuration: new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>() { { "allowInterruptions", "false" } })
                    .Build());
        }

        [Fact]
        public async Task BatchInput_Termination_WithTangent_InterruptionEnabled_WithReprompt()
        {
            await TestUtils.RunTestScript(
                _resourceExplorerFixture.ResourceExplorer,
                adapterChannel: Channels.Telephony,
                configuration: new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>() { { "alwaysPrompt", "true" } })
                    .Build());
        }

        [Fact]
        public async Task BatchInput_FixedLength_WithTangent_InterruptionEnabled()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }

        [Fact]
        public async Task BatchInput_FixedLength_WithTangent_InterruptionEnabled_WithReprompt()
        {
            await TestUtils.RunTestScript(
                _resourceExplorerFixture.ResourceExplorer,
                adapterChannel: Channels.Telephony,
                configuration: new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>() { { "alwaysPrompt", "true" } })
                    .Build());
        }

        [Fact]
        public async Task BatchInput_FixedLength_WithTangent_InterruptionDisabled()
        {
            await TestUtils.RunTestScript(
                _resourceExplorerFixture.ResourceExplorer,
                adapterChannel: Channels.Telephony,
                configuration: new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string>() { { "allowInterruptions", "false" } })
                    .Build());
        }
    }
}
