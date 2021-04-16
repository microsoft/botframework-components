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
    public class GetManagerTests : WhoSkillTestBase<GetManagerTests>
    {
        [TestMethod]
        public async Task GetManager_HappyPath()
        {
            User manager = this.AddUserProfile("Megan Bowen", "megan@contoso.com", "425-111-1111", "City Center", "Software Engineer", addToSearchResult: false);
            User me = this.AddUserProfile(name: "Thomas Chung", email: "thomas@contoso.com", phoneNumber: "425-222-2222", officeLocation: "City Center", jobTitle: "Software Engineer II", addToSearchResult: true);

            // Setup the tree
            this.SetupUserRequest(manager, null, new List<User>() { me });
            this.SetupUserRequest(me, manager, null);

            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetManager_UserNotFound()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetManager_NoManager()
        {
            User me = this.AddUserProfile(name: "Thomas Chung", email: "thomas@contoso.com", phoneNumber: "425-222-2222", officeLocation: "City Center", jobTitle: "Software Engineer II", addToSearchResult: true);
            this.SetupUserRequest(me, null, null);

            await this.RunTestScriptAsync();
        }
    }
}
