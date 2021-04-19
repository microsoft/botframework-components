// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Dialogs.Tests.WhoSkill
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System;
    using System.Threading.Tasks;

    [TestClass]
    public class GetProfileTests : WhoSkillTestBase<GetProfileTests>
    {
        [TestMethod]
        public async Task GetProfile_HappyPath()
        {
            this.SetupUserRequest(this.AddUserProfile(Guid.Parse("017A3513-1033-43D1-993D-42C645518D21"), "Thomas Chung", "thomas@contoso.com", "425-111-1111", "City Center", "Software Engineer"));

            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetProfile_HappyPath_MultipleUsers()
        {
            this.SetupUserRequest(this.AddUserProfile(Guid.Parse("82B9F3C1-03A6-404B-B7FB-B10B67A4F113"), "Thomas A Chung", "thomasa@contoso.com", "425-111-1111", "City Center", "Software Engineer"));
            this.SetupUserRequest(this.AddUserProfile(Guid.Parse("BA692436-3179-41F1-9A15-5CF3B8F39FD0"), "Thomas B Chung", "thomasb@contoso.com", "425-222-2222", "City Center", "Software Engineer II"));
            this.SetupUserRequest(this.AddUserProfile(Guid.Parse("2430238A-7C71-492F-A74F-153D6E50A01D"), "Thomas C Chung", "thomasc@contoso.com", "425-333-3333", "City Center", "PM"));
            this.SetupUserRequest(this.AddUserProfile(Guid.Parse("0C3CEC0A-8238-40DB-B9BB-E18BF34BDFAF"), "Thomas D Chung", "thomasd@contoso.com", "425-444-4444", "City Center", "Designer"));

            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetProfile_UserNotFound()
        {
            await this.RunTestScriptAsync();
        }
    }
}