// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Dialogs.Tests.CalendarSkill
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class UpdateEventTests : CalendarSkillTestBase
    {
        protected override string RelativeTestResourceFolder => Path.Combine(GetProjectPath(), "CalendarSkillTests", nameof(UpdateEventTests));

        [TestMethod]
        public async Task UpdateEvent_basic_attendees()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task UpdateEvent_basic_datetime()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task UpdateEvent_basic_description()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task UpdateEvent_basic_duration()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task UpdateEvent_basic_location()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task UpdateEvent_basic_onlineMeeting()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task UpdateEvent_basic_title()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task UpdateEvent_interruption_setAttendeesAdd()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task UpdateEvent_interruption_setAttendeesRemove()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task UpdateEvent_interruption_setDateTime()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task UpdateEvent_interruption_setDescription()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task UpdateEvent_interruption_setDuration()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task UpdateEvent_interruption_setLocation()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task UpdateEvent_interruption_setOnlineMeeting()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task UpdateEvent_interruption_setTitle()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task UpdateEvent_trigger_setAttendeesAdd()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task UpdateEvent_trigger_setAttendeesRemove()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task UpdateEvent_trigger_setDateTime()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task UpdateEvent_trigger_setDescription()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task UpdateEvent_trigger_setDuration()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task UpdateEvent_trigger_setLocation()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task UpdateEvent_trigger_setOnlineMeeting()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task UpdateEvent_trigger_setTitle()
        {
            await this.RunTestScript();
        }
    }
}