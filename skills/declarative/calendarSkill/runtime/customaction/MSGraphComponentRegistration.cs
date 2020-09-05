using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.BotFramework.Composer.CustomAction.Actions.MSGraph;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.BotFramework.Composer.CustomAction
{
    public class MSGraphComponentRegistration : ComponentRegistration, IComponentDeclarativeTypes
    {
        public IEnumerable<DeclarativeType> GetDeclarativeTypes(ResourceExplorer resourceExplorer)
        {
            // Actions
            yield return new DeclarativeType<GetSettings>(GetSettings.DeclarativeType);
            yield return new DeclarativeType<GetEvents>(GetEvents.DeclarativeType);
            yield return new DeclarativeType<SortEvents>(SortEvents.DeclarativeType);
        }

        public IEnumerable<JsonConverter> GetConverters(ResourceExplorer resourceExplorer, SourceContext sourceContext)
        {
            yield break;
        }
    }
}
