// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Dialogs.Tests.WhoSkill
{
    using Microsoft.Graph;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    [TestClass]
    public class GetDirectReportsTests : WhoSkillTestBase<GetDirectReportsTests>
    {
        [TestMethod]
        public async Task GetDirectReports_OneFound()
        {
            User user = this.AddUserProfile(Guid.Parse("6976FE7F-AA18-4AA4-AECC-05C78DCB37F8"), "Thomas Chung", "thomas@contoso.com", "111-111-1111", "Redwest A", "PM", true);
            User directReport = this.AddUserProfile(Guid.Parse("4D7FDC00-6FDD-4725-B60A-2A2BEFD4CE42"), "John Doe", "john@contoso.com", "222-222-2222", "Redwest B", "Director of PM", false);

            this.SetupUserRequest(user, null, new List<User>() { directReport });
            this.SetupUserRequest(directReport, user, null);

            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetDirectReports_MoreThanOneFound()
        {
            User user = this.AddUserProfile(Guid.Parse("F13BB087-1F7E-4304-9AC5-B57787836983"), "Thomas Chung", "thomas@contoso.com", "111-111-1111", "Redwest A", "PM", true);
            User directReport1 = this.AddUserProfile(Guid.Parse("9CB044E0-478C-4AC6-9E29-1A11A51113C5"), "John Doe", "john@contoso.com", "222-222-2222", "Redwest B", "Director of PM", false);
            User directReport2 = this.AddUserProfile(Guid.Parse("2A2F9560-0DBE-4F74-B783-243AB82411E4"), "Jane Doe", "john@contoso.com", "222-222-2222", "Redwest C", "Director of PM", false);

            this.SetupUserRequest(user, null, new List<User>() { directReport1, directReport2 });
            this.SetupUserRequest(directReport1, user, null);
            this.SetupUserRequest(directReport2, user, null);

            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetDirectReports_UserNotFound()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetDirectReports_DirectReportsNotFound()
        {
            User user = this.AddUserProfile(Guid.Parse("278B7D87-055C-4572-BEFE-7EB4AC5C7F9D"), "Thomas Chung", "thomas@contoso.com", "111-111-1111", "Redwest A", "PM", true);
            this.SetupUserRequest(user, null, null);

            await this.RunTestScriptAsync();
        }
    }
}