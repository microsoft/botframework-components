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
    public class GetPeersTests : WhoSkillTestBase<GetPeersTests>
    {
        [TestMethod]
        public async Task GetPeers_OneFound()
        {
            Profile user = this.AddUserProfile("Thomas Chung", "thomas@contoso.com", "111-111-1111", "Redwest A", "PM", false);
            Profile directReport1 = this.AddUserProfile("John Doe", "john@contoso.com", "222-222-2222", "Redwest B", "Director of PM", true);
            Profile directReport2 = this.AddUserProfile("Jane Doe", "jane@contoso.com", "222-222-2222", "Redwest C", "Director of PM", false);

            this.SetupUserRequest(user, null, new List<Profile>() { directReport1, directReport2 });
            this.SetupUserRequest(directReport1, user, null);
            this.SetupUserRequest(directReport2, user, null);

            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetPeers_MoreThanOneFound()
        {
            Profile user = this.AddUserProfile("Thomas Chung", "thomas@contoso.com", "111-111-1111", "Redwest A", "PM", false);
            Profile directReport1 = this.AddUserProfile("John Doe", "john@contoso.com", "222-222-2222", "Redwest B", "Director of PM", true);
            Profile directReport2 = this.AddUserProfile("Jane Doe", "jane@contoso.com", "333-222-2222", "Redwest C", "Director of PM", false);
            Profile directReport3 = this.AddUserProfile("Joe Doe", "joe@contoso.com", "444-222-2222", "Redwest C", "Director of PM", false);
            Profile directReport4 = this.AddUserProfile("Jack Doe", "jack@contoso.com", "555-222-2222", "Redwest C", "Director of PM", false);

            this.SetupUserRequest(user, null, new List<Profile>() { directReport1, directReport2, directReport3, directReport4 });
            this.SetupUserRequest(directReport1, user, null);
            this.SetupUserRequest(directReport2, user, null);
            this.SetupUserRequest(directReport3, user, null);
            this.SetupUserRequest(directReport4, user, null);

            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetPeers_UserNotFound()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetPeers_PeersNotFound()
        {
            Profile user = this.AddUserProfile("Thomas Chung", "thomas@contoso.com", "111-111-1111", "Redwest A", "PM", true);

            this.SetupUserRequest(user, null, null);

            await this.RunTestScriptAsync();
        }
    }
}
