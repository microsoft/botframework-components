using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Solutions.Extensions.Actions;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Bot.Solutions.Extensions
{
    public class MSGraphComponentRegistration : ComponentRegistration, IComponentDeclarativeTypes
    {
        public IEnumerable<DeclarativeType> GetDeclarativeTypes(ResourceExplorer resourceExplorer)
        {
            // Actions
            yield return new DeclarativeType<GetUsers>(GetUsers.DeclarativeType);
            yield return new DeclarativeType<GetManager>(GetManager.DeclarativeType);
            yield return new DeclarativeType<GetDirectReports>(GetDirectReports.DeclarativeType);
            yield return new DeclarativeType<GetPeers>(GetPeers.DeclarativeType);
            yield return new DeclarativeType<GetEventContacts>(GetEventContacts.DeclarativeType);
            yield return new DeclarativeType<GetEventContactsByKeyword>(GetEventContactsByKeyword.DeclarativeType);
            yield return new DeclarativeType<GetEmailContacts>(GetEmailContacts.DeclarativeType);
            yield return new DeclarativeType<GetEmailContactsByKeyword>(GetEmailContactsByKeyword.DeclarativeType);
            yield return new DeclarativeType<GetMe>(GetMe.DeclarativeType);
        }

        public IEnumerable<JsonConverter> GetConverters(ResourceExplorer resourceExplorer, SourceContext sourceContext)
        {
            yield break;
        }
    }
}
