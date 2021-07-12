// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using Microsoft.Bot.Builder.Dialogs.Declarative.Obsolete;
using Microsoft.Bot.Components.Graph;

namespace Microsoft.Bot.Dialogs.Tests.Common
{
    /// <summary>
    /// Define component assets for Graph.
    /// </summary>
    [Obsolete("Use `GraphBotComponent`.")]
    public class GraphComponentRegistration : DeclarativeComponentRegistrationBridge<GraphBotComponent>
    {
    }
}
