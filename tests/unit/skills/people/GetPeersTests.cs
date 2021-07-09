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
    public class GetPeersTests : WhoSkillTestBase<GetPeersTests>
    {
        [TestMethod]
        public async Task GetPeers_OneFound()
        {
            User user = this.AddUserProfile(Guid.Parse("82AA64F7-D669-4C1B-AD33-D14FB738F406"), "Thomas Chung", "thomas@contoso.com", "111-111-1111", "Redwest A", "PM", false);
            User directReport1 = this.AddUserProfile(Guid.Parse("3D812152-FF44-4FC6-9315-4D0496E29D20"), "John Doe", "john@contoso.com", "222-222-2222", "Redwest B", "Director of PM", true);
            User directReport2 = this.AddUserProfile(Guid.Parse("EE9FBA0A-58D5-4557-97D1-B8CFDED35E7F"), "Jane Doe", "jane@contoso.com", "222-222-2222", "Redwest C", "Director of PM", false);

            this.SetupUserRequest(user, null, new List<User>() { directReport1, directReport2 });
            this.SetupUserRequest(directReport1, user, null);
            this.SetupUserRequest(directReport2, user, null);

            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetPeers_MoreThanOneFound()
        {
            User user = this.AddUserProfile(Guid.Parse("A5E6B3C2-0131-4940-B26A-D6DC760E2CAD"), "Thomas Chung", "thomas@contoso.com", "111-111-1111", "Redwest A", "PM", false);
            User directReport1 = this.AddUserProfile(Guid.Parse("AD9617DE-BB35-4D79-9CD2-8E1E494D40C6"), "John Doe", "john@contoso.com", "222-222-2222", "Redwest B", "Director of PM", true);
            User directReport2 = this.AddUserProfile(Guid.Parse("4716A3CD-EE01-42E7-B258-961B295D9217"), "Jane Doe", "jane@contoso.com", "333-222-2222", "Redwest C", "Director of PM", false);
            User directReport3 = this.AddUserProfile(Guid.Parse("B8EF3F08-8DE6-4456-BF21-78C7159A3255"), "Joe Doe", "joe@contoso.com", "444-222-2222", "Redwest C", "Director of PM", false);
            User directReport4 = this.AddUserProfile(Guid.Parse("D760B88A-D25D-487D-82BC-A13CA31FD01B"), "Jack Doe", "jack@contoso.com", "555-222-2222", "Redwest C", "Director of PM", false);

            this.SetupUserRequest(user, null, new List<User>() { directReport1, directReport2, directReport3, directReport4 });
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
            User user = this.AddUserProfile(Guid.Parse("A6291F25-0E24-43F9-9F5A-6B8F3F7050DB"), "Thomas Chung", "thomas@contoso.com", "111-111-1111", "Redwest A", "PM", true);

            this.SetupUserRequest(user, null, null);

            await this.RunTestScriptAsync();
        }
    }
}