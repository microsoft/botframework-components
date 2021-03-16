// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Dialogs.Tests.CalendarSkill
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class AvailabilityTests : CalendarSkillTestBase
    {
        protected override string RelativeTestResourceFolder => Path.Combine(GetProjectPath(), "CalendarSkillTests", nameof(AvailabilityTests));

        [TestMethod]
        public async Task GetAvailabilityBreaks()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task GetAvailabilityFirst()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task GetAvailabilityLast()
        {
            await this.RunTestScript();
        }
    }
}