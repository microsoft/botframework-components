// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Dialogs.Tests.WhoSkill
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.IO;
    using System.Threading.Tasks;

    [TestClass]
    public class GetProfileTests : WhoSkillTestBase<GetProfileTests>
    {
        [TestMethod]
        public async Task GetProfile_HappyPath()
        {
            this.SetupUserRequest(this.AddUserProfile("Thomas Chung", "thomas@contoso.com", "425-111-1111", "City Center", "Software Engineer"));

            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetProfile_HappyPath_MultipleUsers()
        {
            this.SetupUserRequest(this.AddUserProfile("Thomas A Chung", "thomasa@contoso.com", "425-111-1111", "City Center", "Software Engineer"));
            this.SetupUserRequest(this.AddUserProfile("Thomas B Chung", "thomasb@contoso.com", "425-222-2222", "City Center", "Software Engineer II"));
            this.SetupUserRequest(this.AddUserProfile("Thomas C Chung", "thomasc@contoso.com", "425-333-3333", "City Center", "PM"));
            this.SetupUserRequest(this.AddUserProfile("Thomas D Chung", "thomasd@contoso.com", "425-444-4444", "City Center", "Designer"));

            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetProfile_UserNotFound()
        {
            await this.RunTestScriptAsync();
        }
    }
}
