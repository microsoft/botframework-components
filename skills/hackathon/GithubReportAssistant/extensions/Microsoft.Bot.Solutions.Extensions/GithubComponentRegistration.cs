using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs.Debugging;
using Microsoft.Bot.Builder.Dialogs.Declarative;
using Microsoft.Bot.Builder.Dialogs.Declarative.Resources;
using Microsoft.Bot.Solutions.Extensions.Actions;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Bot.Solutions.Extensions
{
    public class GithubComponentRegistration : ComponentRegistration, IComponentDeclarativeTypes
    {
        public IEnumerable<DeclarativeType> GetDeclarativeTypes(ResourceExplorer resourceExplorer)
        {
            // Actions
            yield return new DeclarativeType<GetIssues>(GetIssues.DeclarativeType);
            yield return new DeclarativeType<FilterIssues>(FilterIssues.DeclarativeType);
            yield return new DeclarativeType<GetIssuesPerDayData>(GetIssuesPerDayData.DeclarativeType);
            yield return new DeclarativeType<GetClosedIssuesPerDayData>(GetClosedIssuesPerDayData.DeclarativeType);
        }

        public IEnumerable<JsonConverter> GetConverters(ResourceExplorer resourceExplorer, SourceContext sourceContext)
        {
            yield break;
        }
    }
}
