// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Dialogs.Tests.CalendarSkill
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CreateEventTests : CalendarSkillTestBase
    {
        /// <inheritdoc/>
        protected override string RelativeTestResourceFolder => Path.Combine(GetProjectPath(), "CalendarSkillTests", nameof(CreateEventTests));

        [TestMethod]
        public async Task CreateEvent()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task CreateEvent_noEntities()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task CreateEvent_withEntity_title()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task CreateEvent_withEntity_title_contact()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task CreateEvent_withEntity_title_contact_datetime()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task CreateEvent_withEntity_contact()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task CreateEvent_withEntity_datetime()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task CreateEvent_withEntity_location()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task CreateEvent_interruption_setTitle()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task CreateEvent_interruption_setAttendeeAdd()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task CreateEvent_interruption_setAttendeeRemove()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task CreateEvent_interruption_setDateTime()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task CreateEvent_interruption_setDescription()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task CreateEvent_interruption_setDuration()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task CreateEvent_interruption_setOnlineMeetingAdd()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task CreateEvent_interruption_setOnlineMeetingRemove()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task CreateEvent_interruption_setLocation()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task CreateEvent_interruption_multiple()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task CreateEvent_interruption_skip()
        {
            await this.RunTestScript();
        }
    }
}