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
            yield return new DeclarativeType<CreateOnlineMeeting>(CreateOnlineMeeting.DeclarativeType);
            yield return new DeclarativeType<GetUserSettings>(GetUserSettings.DeclarativeType);
            yield return new DeclarativeType<FindMeetingTimes>(FindMeetingTimes.DeclarativeType);
            yield return new DeclarativeType<GetContacts>(GetContacts.DeclarativeType);
            yield return new DeclarativeType<GetEvents>(GetEvents.DeclarativeType);
            yield return new DeclarativeType<UpdateEvent>(UpdateEvent.DeclarativeType);
            yield return new DeclarativeType<EventDateTimeInput>(EventDateTimeInput.DeclarativeType);
            yield return new DeclarativeType<SortEvents>(SortEvents.DeclarativeType);
            yield return new DeclarativeType<AcceptEvent>(AcceptEvent.DeclarativeType);
            yield return new DeclarativeType<DeclineEvent>(DeclineEvent.DeclarativeType);
            yield return new DeclarativeType<DeleteEvent>(DeleteEvent.DeclarativeType);
            yield return new DeclarativeType<GetMeetingRooms>(GetMeetingRooms.DeclarativeType);
            yield return new DeclarativeType<GetMeetings>(GetMeetings.DeclarativeType);
            yield return new DeclarativeType<GetSchedule>(GetSchedule.DeclarativeType);
            yield return new DeclarativeType<UpdateEventTime>(UpdateEventTime.DeclarativeType);

            yield return new DeclarativeType<GetUsers>(GetUsers.DeclarativeType);
            yield return new DeclarativeType<GetManager>(GetManager.DeclarativeType);
            yield return new DeclarativeType<GetDirectReports>(GetDirectReports.DeclarativeType);
            yield return new DeclarativeType<GetEventContacts>(GetEventContacts.DeclarativeType);
            yield return new DeclarativeType<GetEventContactsByKeyword>(GetEventContactsByKeyword.DeclarativeType);
            yield return new DeclarativeType<GetEmailContacts>(GetEmailContacts.DeclarativeType);
            yield return new DeclarativeType<GetEmailContactsByKeyword>(GetEmailContactsByKeyword.DeclarativeType);

            // shared
            yield return new DeclarativeType<GetMe>(GetMe.DeclarativeType);
            yield return new DeclarativeType<RetrievePhoto>(RetrievePhoto.DeclarativeType);

            // general
            yield return new DeclarativeType<ResolveTimex>(ResolveTimex.DeclarativeType);
            yield return new DeclarativeType<CustomGotoAction>(CustomGotoAction.Kind);
        }

        public IEnumerable<JsonConverter> GetConverters(ResourceExplorer resourceExplorer, SourceContext sourceContext)
        {
            yield break;
        }
    }
}
