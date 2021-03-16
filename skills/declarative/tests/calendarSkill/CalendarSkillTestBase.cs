// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Dialogs.Tests.CalendarSkill
{
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Components.Calendar;
    using Microsoft.Bot.Components.Graph;
    using Microsoft.Bot.Dialogs.Tests.Common;
    using Microsoft.BotFramework.Composer.CustomAction;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using System.IO;

    [TestClass]
    public abstract class CalendarSkillTestBase : PbxDialogTestBase
    {
        /// <inheritdoc/>
        protected override string RelativeRootFolder => Path.Combine(GetProjectPath(), @"..\..\calendarSkill");

        protected override void InitializeTest()
        {
            ComponentRegistration.Add(new GraphComponentRegistration());
            ComponentRegistration.Add(new CalendarComponentRegistration());
            ComponentRegistration.Add(new CustomActionComponentRegistration());
        }
    }
}
