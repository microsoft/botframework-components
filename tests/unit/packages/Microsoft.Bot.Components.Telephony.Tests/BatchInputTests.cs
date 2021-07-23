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
        public async Task BatchInput_FixedLengthIgnoresNonDTMF()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }
        [Fact]
        public async Task BatchInput_TerminationIgnoresNonDTMF()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }
    }
}
