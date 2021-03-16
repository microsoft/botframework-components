// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Dialogs.Tests.CalendarSkill
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class GetEventTests : CalendarSkillTestBase
    {
        protected override string RelativeTestResourceFolder => Path.Combine(GetProjectPath(), "CalendarSkillTests", nameof(GetEventTests));

        [TestMethod]
        public async Task GetEventAttendees()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task GetEventDateTime()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task GetEventLocation()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task GetEvents_multipleResults()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task GetEvents_noResults()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task GetEvents_singleResult()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task GetEvents_withEntity_contact()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task GetEvents_withEntity_datetime()
        {
            await this.RunTestScript();
        }
    }
}