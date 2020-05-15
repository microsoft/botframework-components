using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Solutions.Extensions.Actions;
using Microsoft.Bot.Solutions.Extensions.Input;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Bot.Solutions.Extensions
{
    public class MSGraphComponentRegistration : ComponentRegistration, IComponentDeclarativeTypes
    {
        public IEnumerable<DeclarativeType> GetDeclarativeTypes(ResourceExplorer resourceExplorer)
        {
            // Actions
            yield return new DeclarativeType<CreateEvent>(CreateEvent.DeclarativeType);
            yield return new DeclarativeType<FindMeetingTimes>(FindMeetingTimes.DeclarativeType);
            yield return new DeclarativeType<GetContacts>(GetContacts.DeclarativeType);
            yield return new DeclarativeType<GetEvents>(GetEvents.DeclarativeType);
            yield return new DeclarativeType<UpdateEvent>(UpdateEvent.DeclarativeType);
            yield return new DeclarativeType<EventDateTimeInput>(EventDateTimeInput.DeclarativeType);
            yield return new DeclarativeType<SortEvents>(SortEvents.DeclarativeType);
            yield return new DeclarativeType<RetrievePhoto>(RetrievePhoto.DeclarativeType);
            yield return new DeclarativeType<AcceptEvent>(AcceptEvent.DeclarativeType);
            yield return new DeclarativeType<DeclineEvent>(DeclineEvent.DeclarativeType);
            yield return new DeclarativeType<DeleteEvent>(DeleteEvent.DeclarativeType);
            yield return new DeclarativeType<GetMe>(GetMe.DeclarativeType);
            yield return new DeclarativeType<GetSchedule>(GetSchedule.DeclarativeType);
            yield return new DeclarativeType<UpdateEventTime>(UpdateEventTime.DeclarativeType);
            yield return new DeclarativeType<GetMeetingRooms>(GetMeetingRooms.DeclarativeType);
        }

        public IEnumerable<JsonConverter> GetConverters(ResourceExplorer resourceExplorer, SourceContext sourceContext)
        {
            yield break;
        }
    }
}
