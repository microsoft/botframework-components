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
    public class GetCollaboratorsTests : WhoSkillTestBase<GetCollaboratorsTests>
    {
        [TestMethod]
        public async Task GetCollaborators_OneFound()
        {
            User user = this.AddUserProfile(Guid.Parse("D614B1A9-D0FE-4117-8A6B-D4AF5E40799C"), "Thomas Chung", "thomas@contoso.com", "111-111-1111", "Redwest A", "PM", true);
            User collaborator1 = this.AddUserProfile(Guid.Parse("E6672029-DB53-431C-8087-2C8355FBBFFC"), "John Doe", "john@contoso.com", "222-222-2222", "Redwest B", "Director of PM", false);

            this.SetupUserRequest(user, null, null, new List<User>() { collaborator1 });
            this.SetupUserRequest(collaborator1, null, null, new List<User>() { user });

            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetCollaborators_MoreThanOneFound()
        {
            User user = this.AddUserProfile(Guid.Parse("DB203EBA-F1C0-421B-BBF9-7A5BA4F809F0"), "Thomas Chung", "thomas@contoso.com", "111-111-1111", "Redwest A", "PM", true);
            User collaborator1 = this.AddUserProfile(Guid.Parse("CF56A5A3-8402-404D-9CE2-0A075581292B"), "John Doe", "john@contoso.com", "222-222-2222", "Redwest B", "Director of PM", false);
            User collaborator2 = this.AddUserProfile(Guid.Parse("7836CB48-F514-41A8-BF48-2FF07CDD6BC9"), "Jane Doe", "john@contoso.com", "333-222-2222", "Redwest B", "Director of PM", false);
            User collaborator3 = this.AddUserProfile(Guid.Parse("B7DDA5F1-4A4B-4A38-9126-C0634A0BB96C"), "Joe Doe", "john@contoso.com", "444-222-2222", "Redwest B", "Director of PM", false);
            User collaborator4 = this.AddUserProfile(Guid.Parse("61C4B0BE-2647-4505-9CF2-A690D917C639"), "Jones Doe", "john@contoso.com", "555-222-2222", "Redwest B", "Director of PM", false);

            this.SetupUserRequest(user, null, null, new List<User>() { collaborator1, collaborator2, collaborator3, collaborator4 });
            this.SetupUserRequest(collaborator1, null, null, new List<User>() { user, collaborator2, collaborator3, collaborator4 });
            this.SetupUserRequest(collaborator2, null, null, new List<User>() { user, collaborator1, collaborator3, collaborator4 });
            this.SetupUserRequest(collaborator3, null, null, new List<User>() { user, collaborator1, collaborator2, collaborator4 });
            this.SetupUserRequest(collaborator4, null, null, new List<User>() { user, collaborator1, collaborator2, collaborator3 });

            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetCollaborators_CollaboratorNotFound()
        {
            User user = this.AddUserProfile(Guid.Parse("EF03E831-6A1E-49B3-BA8F-85A9732C79AE"), "Thomas Chung", "thomas@contoso.com", "111-111-1111", "Redwest A", "PM", true);

            this.SetupUserRequest(user, null, null, null);

            await this.RunTestScriptAsync();
        }
    }
}