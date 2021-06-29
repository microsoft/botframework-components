// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.IO;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Testing;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Obsolete;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Xunit;

namespace Microsoft.Bot.Components.Teams.Tests
{
    public class ConditionalTests
    {
        public ConditionalTests()
        {
            ComponentRegistration.Add(new DeclarativeComponentRegistration());
            ComponentRegistration.Add(new DialogsComponentRegistration());
            ComponentRegistration.Add(new AdaptiveComponentRegistration());
            ComponentRegistration.Add(new LanguageGenerationComponentRegistration());
            ComponentRegistration.Add(new AdaptiveTestingComponentRegistration());
            ComponentRegistration.Add(new DeclarativeComponentRegistrationBridge<TeamsBotComponent>());

            ResourceExplorer = new ResourceExplorer()
                .AddFolder(Path.Combine(TestUtils.GetProjectPath(), "Tests", nameof(ConditionalTests)), monitorChanges: false);
        }

        public static ResourceExplorer ResourceExplorer { get; set; }

        [Fact]
        public async Task ConditionalsTests_OnTeamsActivityTypes()
        {
            await TestUtils.RunTestScript(ResourceExplorer);
        }
    }
}
