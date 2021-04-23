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
    public class GetManagerTests : WhoSkillTestBase<GetManagerTests>
    {
        [TestMethod]
        public async Task GetManager_HappyPath()
        {
            User manager = this.AddUserProfile(Guid.Parse("A700C092-BCD0-41F6-9E05-B52EFCF56A1B"), "Megan Bowen", "megan@contoso.com", "425-111-1111", "City Center", "Software Engineer", addToSearchResult: false);
            User me = this.AddUserProfile(id: Guid.Parse("935F2659-50AB-40ED-AAD2-F43F3885AED7"), name: "Thomas Chung", email: "thomas@contoso.com", phoneNumber: "425-222-2222", officeLocation: "City Center", jobTitle: "Software Engineer II", addToSearchResult: true);

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
            User me = this.AddUserProfile(id: Guid.Parse("4F4581BE-775F-4EAA-A8B4-C2267CAED218"), name: "Thomas Chung", email: "thomas@contoso.com", phoneNumber: "425-222-2222", officeLocation: "City Center", jobTitle: "Software Engineer II", addToSearchResult: true);
            this.SetupUserRequest(me, null, null);

            await this.RunTestScriptAsync();
        }
    }
}