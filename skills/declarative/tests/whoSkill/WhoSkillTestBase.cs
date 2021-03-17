// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Dialogs.Tests.WhoSkill
{
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Components.Graph;
    using Microsoft.Bot.Dialogs.Tests.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.IO;

    [TestClass]
    public abstract class WhoSkillTestBase : PbxDialogTestBase
    {
        /// <inheritdoc />
        protected override string RelativeRootFolder => Path.Combine(GetProjectPath(), @"..\..\whoSkill");

        /// <inheritdoc />
        protected override void InitializeTest()
        {
            ComponentRegistration.Add(new GraphComponentRegistration());
        }
    }
}
