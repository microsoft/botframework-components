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
    public class CallTransferTests : IntegrationTestsBase
    {
        public CallTransferTests(ResourceExplorerFixture resourceExplorerFixture) : base(resourceExplorerFixture)
        {
        }

        [Fact]
        public async Task CallTransfer_HappyPath()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }

        [Fact]
        public async Task CallTransfer_EmptyPhoneNumber()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        }

        [Fact]
        public async Task CallTransfer_IgnoredInNonTelephonyChannel()
        {
            await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Msteams);
        }
    }
}
