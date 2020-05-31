// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Solutions.Extensions.Actions;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Bot.Solutions.Extensions
{
    public class ManageNotificationRegistration : ComponentRegistration, IComponentDeclarativeTypes
    {
        public IEnumerable<DeclarativeType> GetDeclarativeTypes(ResourceExplorer resourceExplorer)
        {
            // Actions
            yield return new DeclarativeType<ManageNotification>(ManageNotification.DeclarativeType);
            yield return new DeclarativeType<GithubNotification>(GithubNotification.DeclarativeType);
        }

        public IEnumerable<JsonConverter> GetConverters(ResourceExplorer resourceExplorer, SourceContext sourceContext)
        {
            yield break;
        }
    }
}
