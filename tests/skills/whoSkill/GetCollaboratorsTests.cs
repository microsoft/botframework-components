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
    public class GetCollaboratorsTests : WhoSkillTestBase<GetCollaboratorsTests>
    {
        [TestMethod]
        public async Task GetCollaborators_OneFound()
        {
            User user = this.AddUserProfile("Thomas Chung", "thomas@contoso.com", "111-111-1111", "Redwest A", "PM", true);
            User collaborator1 = this.AddUserProfile("John Doe", "john@contoso.com", "222-222-2222", "Redwest B", "Director of PM", false);

            this.SetupUserRequest(user, null, null, new List<User>() { collaborator1 });

            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetCollaborators_MoreThanOneFound()
        {
            User user = this.AddUserProfile("Thomas Chung", "thomas@contoso.com", "111-111-1111", "Redwest A", "PM", true);
            User collaborator1 = this.AddUserProfile("John Doe", "john@contoso.com", "222-222-2222", "Redwest B", "Director of PM", false);
            User collaborator2 = this.AddUserProfile("Jane Doe", "john@contoso.com", "333-222-2222", "Redwest B", "Director of PM", false);
            User collaborator3 = this.AddUserProfile("Joe Doe", "john@contoso.com", "444-222-2222", "Redwest B", "Director of PM", false);
            User collaborator4 = this.AddUserProfile("Jones Doe", "john@contoso.com", "555-222-2222", "Redwest B", "Director of PM", false);

            this.SetupUserRequest(user, null, null, new List<User>() { collaborator1, collaborator2, collaborator3, collaborator4 });

            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetCollaborators_CollaboratorNotFound()
        {
            User user = this.AddUserProfile("Thomas Chung", "thomas@contoso.com", "111-111-1111", "Redwest A", "PM", true);

            this.SetupUserRequest(user, null, null, null);

            await this.RunTestScriptAsync();
        }
    }
}
