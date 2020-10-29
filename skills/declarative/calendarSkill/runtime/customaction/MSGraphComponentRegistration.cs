using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.BotFramework.Composer.CustomAction.Actions;
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
            yield return new DeclarativeType<GetProfile>(GetProfile.DeclarativeType);
            yield return new DeclarativeType<GetEvents>(GetEvents.DeclarativeType);
            yield return new DeclarativeType<SortEvents>(SortEvents.DeclarativeType);
            yield return new DeclarativeType<GetWorkingHours>(GetWorkingHours.DeclarativeType);
            yield return new DeclarativeType<GetContacts>(GetContacts.DeclarativeType);
            yield return new DeclarativeType<FindMeetingTimes>(FindMeetingTimes.DeclarativeType);
            yield return new DeclarativeType<CreateEvent>(CreateEvent.DeclarativeType);
            yield return new DeclarativeType<AcceptEvent>(AcceptEvent.DeclarativeType);
            yield return new DeclarativeType<TentativelyAcceptEvent>(TentativelyAcceptEvent.DeclarativeType);
            yield return new DeclarativeType<DeclineEvent>(DeclineEvent.DeclarativeType);
            yield return new DeclarativeType<DeleteEvent>(DeleteEvent.DeclarativeType);
            yield return new DeclarativeType<CheckAvailability>(CheckAvailability.DeclarativeType);
            yield return new DeclarativeType<FindAvailableTime>(FindAvailableTime.DeclarativeType);
        }

        public IEnumerable<JsonConverter> GetConverters(ResourceExplorer resourceExplorer, SourceContext sourceContext)
        {
            yield break;
        }
    }
}
