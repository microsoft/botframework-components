// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Components.Calendar.Actions;
using Newtonsoft.Json;

namespace Microsoft.Bot.Components.Calendar
{
    public class CalendarComponentRegistration : ComponentRegistration, IComponentDeclarativeTypes
    {
        public IEnumerable<DeclarativeType> GetDeclarativeTypes(ResourceExplorer resourceExplorer)
        {
            // Actions
            yield return new DeclarativeType<GotoAction>(GotoAction.DeclarativeType);
            yield return new DeclarativeType<RecognizeDateTime>(RecognizeDateTime.DeclarativeType);
            yield return new DeclarativeType<SendActivityPlus>(SendActivityPlus.Kind);
            yield return new DeclarativeType<CheckAvailability>(CheckAvailability.DeclarativeType);
            yield return new DeclarativeType<FindAvailableTime>(FindAvailableTime.DeclarativeType);
            yield return new DeclarativeType<GroupEventsByDate>(GroupEventsByDate.DeclarativeType);
        }

        public IEnumerable<JsonConverter> GetConverters(ResourceExplorer resourceExplorer, SourceContext sourceContext)
        {
            yield break;
        }
    }
}
