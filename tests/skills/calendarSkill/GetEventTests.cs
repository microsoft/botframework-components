// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Dialogs.Tests.CalendarSkill
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class GetEventTests : CalendarSkillTestBase<GetEventTests>
    {
        [TestMethod]
        public async Task GetEventAttendees()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetEventDateTime()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetEventLocation()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetEvents_multipleResults()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetEvents_noResults()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetEvents_singleResult()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetEvents_withEntity_contact()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetEvents_withEntity_datetime()
        {
            await this.RunTestScriptAsync();
        }
    }
}