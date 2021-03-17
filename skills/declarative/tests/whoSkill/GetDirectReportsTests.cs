// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Dialogs.Tests.WhoSkill
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.IO;

    [TestClass]
    public class GetDirectReportsTests : WhoSkillTestBase
    {
        protected override string RelativeTestResourceFolder => Path.Combine(GetProjectPath(), nameof(GetDirectReportsTests));

        [TestMethod]
        public void GetDirectReports_OneFoundFound()
        {
        }

        [TestMethod]
        public void GetDirectReports_MoreThanOneFound()
        {
        }

        [TestMethod]
        public void GetDirectReports_UserNotFound()
        {
        }

        [TestMethod]
        public void GetDirectReports_DirectReportsNotFound()
        {
        }
    }
}
