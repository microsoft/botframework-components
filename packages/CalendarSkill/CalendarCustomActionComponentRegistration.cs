using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Component.CalendarSkill.Actions;
using Microsoft.Bot.Component.CalendarSkill.Actions.MSGraph;
using Microsoft.Bot.Solutions.Extensions.Actions;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Bot.Component.CalendarSkill
{
    public class CalendarCustomActionComponentRegistration : ComponentRegistration, IComponentDeclarativeTypes
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
