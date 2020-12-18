using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Solutions.Extensions.Actions;
using Microsoft.BotFramework.Composer.CustomAction.Actions;
using Microsoft.BotFramework.Composer.CustomAction.Actions.MSGraph;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.BotFramework.Composer.CustomAction
{
    public class CustomActionComponentRegistration : ComponentRegistration, IComponentDeclarativeTypes
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
