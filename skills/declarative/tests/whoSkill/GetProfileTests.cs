// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Dialogs.Tests.WhoSkill
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.IO;
    using System.Threading.Tasks;

    [TestClass]
    public class GetProfileTests : WhoSkillTestBase
    {
        protected override string RelativeTestResourceFolder => Path.Combine(GetProjectPath(), nameof(GetProfileTests));

        [TestMethod]
        public async Task GetProfile_HappyPath()
        {
            await this.RunTestScriptAsync();
        }

        [TestMethod]
        public async Task GetProfile_UserNotFound()
        {
            await this.RunTestScriptAsync();
        }
    }
}
