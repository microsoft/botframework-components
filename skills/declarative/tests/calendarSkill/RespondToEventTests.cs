// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Dialogs.Tests.CalendarSkill
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RespondToEventTests : CalendarSkillTestBase
    {
        protected override string RelativeTestResourceFolder => Path.Combine(GetProjectPath(), "CalendarSkillTests", nameof(RespondToEventTests));

        [TestMethod]
        public async Task RespondToEvent_Accept()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task RespondToEvent_Decline()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task RespondToEvent_TentativelyAccept()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task CancelEvent_asAttendee()
        {
            await this.RunTestScript();
        }

        [TestMethod]
        public async Task CancelEvent_asOrganizer()
        {
            await this.RunTestScript();
        }
    }
}