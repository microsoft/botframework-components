// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Dialogs.Tests.CalendarSkill
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AvailabilityTests : CalendarSkillTestBase<AvailabilityTests>
    {
        [TestMethod]
        public async Task GetAvailabilityBreaks()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetAvailabilityFirst()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetAvailabilityLast()
        {
            await this.RunTestScriptAsync();
        }
    }
}