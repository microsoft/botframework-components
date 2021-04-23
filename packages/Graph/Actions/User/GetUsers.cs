// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.Graph.Actions
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveExpressions.Properties;
    using Microsoft.Bot.Builder.Dialogs.Memory;
    using Microsoft.Bot.Components.Graph;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// Find users using Microsoft Graph.
    /// </summary>
    [GraphCustomActionRegistration(GetUsers.FindUsersDeclarativeType)]
    public class GetUsers : BaseMsGraphCustomAction<IEnumerable<User>>
    {
        /// <summary>
        /// Declarative type for the custom action.
        /// </summary>
        public const string FindUsersDeclarativeType = "Microsoft.Graph.User.GetUsers";

        /// <summary>
        /// Default max number of results to return.
        /// </summary>
        private const int DefaultMaxCount = 0;

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
        public IntExpression MaxCountProperty { get; set; }

        /// <summary>
        /// Gets or sets the properties to select from the Graph API.
        /// </summary>
        [JsonProperty("PropertiesToSelect")]
        public ArrayExpression<string> PropertiesToSelect { get; set; }

        /// <inheritdoc/>
        public override string DeclarativeType => GetUsers.FindUsersDeclarativeType;

        /// <inheritdoc/>
        internal override async Task<IEnumerable<User>> CallGraphServiceWithResultAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            string nameToSearch = (string)parameters["NameToSearch"];
            int maxCount = (int)parameters["MaxResults"];
            string propertiesToSelect = (string)parameters["PropertiesToSelect"];

            string filterClause = $"startsWith(displayName, '{nameToSearch}') or startsWith(surname, '{nameToSearch}') or startsWith(givenname, '{nameToSearch}')";

            IGraphServiceUsersCollectionRequest request = client.Users.Request().Filter(filterClause).Select(propertiesToSelect);

            // Apply the Top() filter if there is a value to apply
            if (maxCount > 0)
            {
                request = request.Top(maxCount);
            }

            IGraphServiceUsersCollectionPage result = await request.GetAsync(cancellationToken).ConfigureAwait(false);

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

            if (this.MaxCountProperty != null)
            {
                // The TryParse will reset the value to 0 if parse fail
                maxCount = this.MaxCountProperty.GetValue(state);
            }

            // Select minimum of the "id" field from the object
            string propertiesToSelect = DefaultIdField;

            if (this.PropertiesToSelect != null)
            {
                List<string> propertiesFound = this.PropertiesToSelect.GetValue(state);

                if (propertiesFound != null && propertiesFound.Count > 0)
                {
                    propertiesToSelect = string.Join(",", propertiesFound);
                }
            }

            parameters.Add("NameToSearch", nameToSearch);
            parameters.Add("MaxResults", maxCount);
            parameters.Add("PropertiesToSelect", propertiesToSelect);
        }
    }
}
