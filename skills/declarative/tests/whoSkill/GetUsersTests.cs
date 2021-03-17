// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Dialogs.Tests.WhoSkill
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.IO;

    [TestClass]
    public class GetUsersTests : WhoSkillTestBase
    {
        protected override string RelativeTestResourceFolder => Path.Combine(GetProjectPath(), nameof(GetUsersTests));

        [TestMethod]
        public void GetUsers_OneFound()
        {
        }

        [TestMethod]
        public void GetUsers_MoreThanOneFound()
        {
        }

        [TestMethod]
        public void GetUsers_MoreThan15Found()
        {
        }

        [TestMethod]
        public void GetUsers_UserNotFound()
        {
        }
    }
}
