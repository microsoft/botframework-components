// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Dialogs.Tests.CalendarSkill
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CreateEventTests : CalendarSkillTestBase<CreateEventTests>
    {
        [TestMethod]
        public async Task CreateEvent()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CreateEvent_noEntities()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CreateEvent_withEntity_title()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CreateEvent_withEntity_title_contact()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CreateEvent_withEntity_title_contact_datetime()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CreateEvent_withEntity_contact()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CreateEvent_withEntity_datetime()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CreateEvent_withEntity_location()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CreateEvent_interruption_setTitle()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CreateEvent_interruption_setAttendeeAdd()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CreateEvent_interruption_setAttendeeRemove()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CreateEvent_interruption_setDateTime()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CreateEvent_interruption_setDescription()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CreateEvent_interruption_setDuration()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CreateEvent_interruption_setOnlineMeetingAdd()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CreateEvent_interruption_setOnlineMeetingRemove()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CreateEvent_interruption_setLocation()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CreateEvent_interruption_multiple()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CreateEvent_interruption_skip()
        {
            await this.RunTestScriptAsync();
        }
    }
}