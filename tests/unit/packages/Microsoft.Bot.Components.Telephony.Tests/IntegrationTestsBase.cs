// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Testing;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Obsolete;
using Xunit;

namespace Microsoft.Bot.Components.Telephony.Tests
{
    public class IntegrationTestsBase : IClassFixture<ResourceExplorerFixture>
    {
        protected readonly ResourceExplorerFixture _resourceExplorerFixture;

        public IntegrationTestsBase(ResourceExplorerFixture resourceExplorerFixture)
        {
            ComponentRegistration.Add(new DeclarativeComponentRegistration());
            ComponentRegistration.Add(new DialogsComponentRegistration());
            ComponentRegistration.Add(new AdaptiveComponentRegistration());
            ComponentRegistration.Add(new LanguageGenerationComponentRegistration());
            ComponentRegistration.Add(new AdaptiveTestingComponentRegistration());
            ComponentRegistration.Add(new DeclarativeComponentRegistrationBridge<TelephonyBotComponent>());

            _resourceExplorerFixture = resourceExplorerFixture.Initialize(this.GetType().Name);
        }
    }
}
