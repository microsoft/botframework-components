// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Dialogs.Tests.CalendarSkill
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CustomActionTests : CalendarSkillTestBase<CustomActionTests>
    { 
        [TestMethod]
        public async Task CalendarSkillTests_GetProfile()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CalendarSkillTests_AcceptEvent()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CalendarSkillTests_TentativelyAcceptEvent()
        {
            await this.RunTestScriptAsync();
        }


        [TestMethod]
        public async Task CalendarSkillTests_DeclineEvent()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CalendarSkillTests_DeleteEvent()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CalendarSkillTests_GetEvents()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CalendarSkillTests_GetWorkingHours()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CalendarSkillTests_SettingsTest()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CalendarSkillTests_CreateEvent()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CalendarSkillTests_UpdateEvent()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CalendarSkillTests_GetContacts()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CalendarSkillTests_FindMeetingTimes()
        {
            await this.RunTestScriptAsync();
        }
    }
}
