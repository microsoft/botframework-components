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
    public class TimeoutTests : IntegrationTestsBase
    {
        public TimeoutTests(ResourceExplorerFixture resourceExplorerFixture) : base(resourceExplorerFixture)
        {
        }

        //[Fact]
        //public async Task Timeout_HappyPath()
        //{
        //    await TestUtils.RunTestScript(_resourceExplorerFixture.ResourceExplorer, adapterChannel: Channels.Telephony);
        //}
    }
}
