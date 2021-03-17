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
    public class GetCollaboratorsTests : WhoSkillTestBase
    {
        protected override string RelativeTestResourceFolder => Path.Combine(GetProjectPath(), nameof(GetCollaboratorsTests));

        [TestMethod]
        public async Task GetCollaborators_OneFound()
        {
            Profile user = this.AddUserProfile("Thomas Chung", "thomas@contoso.com", "111-111-1111", "Redwest A", "PM", true);
            Profile collaborator1 = this.AddUserProfile("John Doe", "john@contoso.com", "222-222-2222", "Redwest B", "Director of PM", false);

            this.SetupUserRequest(user, null, null, new List<Profile>() { collaborator1 });

            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetCollaborators_MoreThanOneFound()
        {
            Profile user = this.AddUserProfile("Thomas Chung", "thomas@contoso.com", "111-111-1111", "Redwest A", "PM", true);
            Profile collaborator1 = this.AddUserProfile("John Doe", "john@contoso.com", "222-222-2222", "Redwest B", "Director of PM", false);
            Profile collaborator2 = this.AddUserProfile("Jane Doe", "john@contoso.com", "333-222-2222", "Redwest B", "Director of PM", false);
            Profile collaborator3 = this.AddUserProfile("Joe Doe", "john@contoso.com", "444-222-2222", "Redwest B", "Director of PM", false);
            Profile collaborator4 = this.AddUserProfile("Jones Doe", "john@contoso.com", "555-222-2222", "Redwest B", "Director of PM", false);

            this.SetupUserRequest(user, null, null, new List<Profile>() { collaborator1, collaborator2, collaborator3, collaborator4 });

            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetCollaborators_CollaboratorNotFound()
        {
            Profile user = this.AddUserProfile("Thomas Chung", "thomas@contoso.com", "111-111-1111", "Redwest A", "PM", true);

            this.SetupUserRequest(user, null, null, null);

            await this.RunTestScriptAsync();
        }
    }
}
