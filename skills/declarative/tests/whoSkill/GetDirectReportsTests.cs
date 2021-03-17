// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Dialogs.Tests.WhoSkill
{
    using Microsoft.Graph;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    [TestClass]
    public class GetDirectReportsTests : WhoSkillTestBase
    {
        protected override string RelativeTestResourceFolder => Path.Combine(GetProjectPath(), nameof(GetDirectReportsTests));

        [TestMethod]
        public async Task GetDirectReports_OneFound()
        {
            Profile user = this.AddUserProfile("Thomas Chung", "thomas@contoso.com", "111-111-1111", "Redwest A", "PM", true);
            Profile directReport = this.AddUserProfile("John Doe", "john@contoso.com", "222-222-2222", "Redwest B", "Director of PM", false);

            this.SetupUserRequest(user, null, new List<Profile>() { directReport });
            this.SetupUserRequest(directReport, user, null);

            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetDirectReports_MoreThanOneFound()
        {
            Profile user = this.AddUserProfile("Thomas Chung", "thomas@contoso.com", "111-111-1111", "Redwest A", "PM", true);
            Profile directReport1 = this.AddUserProfile("John Doe", "john@contoso.com", "222-222-2222", "Redwest B", "Director of PM", false);
            Profile directReport2 = this.AddUserProfile("Jane Doe", "john@contoso.com", "222-222-2222", "Redwest C", "Director of PM", false);

            this.SetupUserRequest(user, null, new List<Profile>() { directReport1, directReport2 });
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
            Profile user = this.AddUserProfile("Thomas Chung", "thomas@contoso.com", "111-111-1111", "Redwest A", "PM", true);
            this.SetupUserRequest(user, null, null);

            await this.RunTestScriptAsync();
        }
    }
}
