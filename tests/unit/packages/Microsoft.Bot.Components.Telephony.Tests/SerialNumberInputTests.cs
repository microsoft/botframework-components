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
    public class SerialNumberInputTests : IntegrationTestsBase
    {
        public SerialNumberInputTests(ResourceExplorerFixture resourceExplorerFixture) : base(resourceExplorerFixture)
        {
        }

        [Fact]
        public async Task SerialNumberInput_HappyPath()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }

        [Fact]
        public async Task SerialNumberInput_WithInterruption()
        {
            await TestUtils.RunTestScript(
               _resourceExplorerFixture.ResourceExplorer,
               adapterChannel: Channels.Telephony,
               configuration: new ConfigurationBuilder()
                   .AddInMemoryCollection(new Dictionary<string, string>() { { "alwaysPrompt", "true" } })
                   .Build());
        }

        [Fact]
        public async Task SerialNumberInput_DisregardsInputsLongerThanBatchLength()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }

        [Fact]
        public async Task SerialNumberInput_DisregardAggregationWithInputIfLongerThanBatchLength()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }

        [Fact]
        public async Task SerialNumberInput_IgnoresAlphabetWhenConfigured()
        {
            await TestUtils.RunTestScript(
               _resourceExplorerFixture.ResourceExplorer,
               adapterChannel: Channels.Telephony,
               configuration: new ConfigurationBuilder()
                   .AddInMemoryCollection(new Dictionary<string, string>() { { "acceptAlphabet", "false" } })
                   .Build());
        }

        [Fact]
        public async Task SerialNumberInput_IgnoresNumbersWhenConfigured()
        {
            await TestUtils.RunTestScript(
               _resourceExplorerFixture.ResourceExplorer,
               adapterChannel: Channels.Telephony,
               configuration: new ConfigurationBuilder()
                   .AddInMemoryCollection(new Dictionary<string, string>() { { "acceptNumbers", "false" } })
                   .Build());
        }

        [Fact]
        public async Task SerialNumberInput_UnexpectedInputsUntilInterruptionsAreAllowed()
        {
            await TestUtils.RunTestScript(
               _resourceExplorerFixture.ResourceExplorer,
               adapterChannel: Channels.Telephony,
               configuration: new ConfigurationBuilder()
                   .AddInMemoryCollection(new Dictionary<string, string>() { { "alwaysPrompt", "true" }, { "allowInterruptions", "false" } })
                   .Build());
        }

        [Fact]
        public async Task SerialNumberInput_UnexpectedInputsWorksPerDialogInstance()
        {
            await TestUtils.RunTestScript(
               _resourceExplorerFixture.ResourceExplorer,
               adapterChannel: Channels.Telephony,
               configuration: new ConfigurationBuilder()
                   .AddInMemoryCollection(new Dictionary<string, string>() { { "alwaysPrompt", "true" }, { "allowInterruptions", "false" } })
                   .Build());
        }

        [Fact]
        public async Task SerialNumberInput_UnexpectedInputCountDoesNotPreventSuccessfulCompletion()
        {
            await TestUtils.RunTestScript(
               _resourceExplorerFixture.ResourceExplorer,
               adapterChannel: Channels.Telephony,
               configuration: new ConfigurationBuilder()
                   .AddInMemoryCollection(new Dictionary<string, string>() { { "alwaysPrompt", "true" }, { "allowInterruptions", "false" } })
                   .Build());
        }
    }
}