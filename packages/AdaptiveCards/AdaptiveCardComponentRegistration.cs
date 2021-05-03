// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Bot.Builder.Dialogs.Declarative.Obsolete;

namespace Microsoft.Bot.Components.AdaptiveCards
{
    /// <summary>
    /// <see cref="AdaptiveCardComponentRegistration"/> implementation for legacy runtimes.
    /// </summary>
    [Obsolete("AdaptiveBotComponent is the new component definition.")]
    public class AdaptiveCardComponentRegistration 
        : DeclarativeComponentRegistrationBridge<AdaptiveCardsBotComponent>
    {
    }
}