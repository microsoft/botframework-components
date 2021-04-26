// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Dialogs.Tests.CalendarSkill
{
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Components.Graph;
    using Microsoft.Bot.Dialogs.Tests.Common;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public abstract class CalendarSkillTestBase<T> : PbxDialogTestBase<T>, IHaveComponentsToInitialize 
        where T : IHaveComponentsToInitialize, new()
    {
        public void InitializeComponents()
        {
#pragma warning disable CS0618 // Type or member is obsolete
            ComponentRegistration.Add(new GraphComponentRegistration());
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
