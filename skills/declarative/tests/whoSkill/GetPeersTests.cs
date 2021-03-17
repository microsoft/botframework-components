

namespace Microsoft.Bot.Dialogs.Tests.WhoSkill
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.IO;

    [TestClass]
    public class GetPeersTests : WhoSkillTestBase
    {
        protected override string RelativeTestResourceFolder => Path.Combine(GetProjectPath(), nameof(GetPeersTests));

        [TestMethod]
        public void GetPeers_OneFoundFound()
        {
        }

        [TestMethod]
        public void GetPeers_MoreThanOneFound()
        {
        }

        [TestMethod]
        public void GetPeers_UserNotFound()
        {
        }

        [TestMethod]
        public void GetPeers_PeersNotFound()
        {
        }
    }
}
