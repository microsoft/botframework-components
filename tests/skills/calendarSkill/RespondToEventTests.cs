// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Dialogs.Tests.CalendarSkill
{
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class RespondToEventTests : CalendarSkillTestBase<RespondToEventTests>
    {
        [TestMethod]
        public async Task RespondToEvent_Accept()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task RespondToEvent_Decline()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task RespondToEvent_TentativelyAccept()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CancelEvent_asAttendee()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task CancelEvent_asOrganizer()
        {
            await this.RunTestScriptAsync();
        }
    }
}