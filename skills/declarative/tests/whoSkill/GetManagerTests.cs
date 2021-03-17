// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Dialogs.Tests.WhoSkill
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.IO;

    [TestClass]
    public class GetManagerTests : WhoSkillTestBase
    {
        protected override string RelativeTestResourceFolder => Path.Combine(GetProjectPath(), nameof(GetManagerTests));

        [TestMethod]
        public void GetManager_HappyPath()
        {
        }

        [TestMethod]
        public void GetManager_UserNotFound()
        {
        }

        [TestMethod]
        public void GetManager_NoManager()
        {
        }
    }
}
