// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.MsGraph.Actions
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveExpressions.Properties;
    using Microsoft.Bot.Builder.Dialogs.Memory;
    using Microsoft.Bot.Component.MsGraph.Actions.MSGraph;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// Find users using Microsoft Graph
    /// </summary>
    public class FindUsers : BaseMsGraphCustomAction<IEnumerable<User>>
    {
        /// <summary>
        /// Declarative type for the custom action.
        /// </summary>
        public const string FindUsersDeclarativeType = "Microsoft.Graph.Calendar.FindUsers";

        /// <summary>
        /// Default max number of results to return.
        /// </summary>
        public const int DefaultMaxCount = 15;

        /// <summary>
        /// Gets or sets the name to search for.
        /// </summary>
        /// <value>The timezone for the search query.</value>
        [JsonProperty("NameToSearchFor")]
        public StringExpression NameToSearchForProperty { get; set; }

        /// <summary>
        /// Gets or sets the max number of results to return.
        /// </summary>
        [JsonProperty("MaxCount")]
        public StringExpression MaxCountProperty { get; set; }

        /// <inheritdoc/>
        public override string DeclarativeType => FindUsers.FindUsersDeclarativeType;

        /// <inheritdoc/>
        internal override async Task<IEnumerable<User>> CallGraphServiceWithResultAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            string nameToSearch = (string)parameters["NameToSearch"];
            int maxCount = (int)parameters["MaxResults"];

            string filterClause = $"startsWith(displayName, '{nameToSearch}') or startsWith(surname, '{nameToSearch}') or startsWith(givenname, '{nameToSearch}')";

            IGraphServiceUsersCollectionPage result = await client.Users.Request().Filter(filterClause).Top(maxCount).GetAsync(cancellationToken);

            // The "Top" clause in Graph is just more about max number of results per page.
            // This is unlike SQL where by the results are capped to max. In this case we will just
            // take the result from the first page and don't move on.
            return result.CurrentPage;
        }

        /// <inheritdoc />
        protected override void PopulateParameters(DialogStateManager state, Dictionary<string, object> parameters)
        {
            string nameToSearch = this.NameToSearchForProperty.GetValue(state);
            int maxCount = DefaultMaxCount;

            if (this.MaxCountProperty == null || !int.TryParse(this.MaxCountProperty.GetValue(state), out maxCount))
            {
                // The TryParse will reset the value to 0 if parse fail
                maxCount = DefaultMaxCount;
            }

            parameters.Add("NameToSearch", nameToSearch);
            parameters.Add("MaxResults", maxCount);
        }
    }
}
