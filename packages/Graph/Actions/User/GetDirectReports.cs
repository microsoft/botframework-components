// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.Graph.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using AdaptiveExpressions.Properties;
    using Microsoft.Bot.Builder.Dialogs.Memory;
    using Microsoft.Bot.Components.Graph;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// Get profile of a user's manager in Graph.
    /// </summary>
    [GraphCustomActionRegistration(GetDirectReports.GetDirectReportsDeclarativeType)]
    public class GetDirectReports : BaseMsGraphCustomAction<IEnumerable<DirectoryObject>>
    {
        /// <summary>
        /// Declarative type for the custom action.
        /// </summary>
        private const string GetDirectReportsDeclarativeType = "Microsoft.Graph.User.GetDirectReports";

        /// <summary>
        /// Default max number of results to return.
        /// </summary>
        private const int DefaultMaxCount = 0;

        /// <summary>
        /// Gets or sets the max number of results to return.
        /// </summary>
        [JsonProperty("UserId")]
        public StringExpression UserId { get; set; }

        /// <summary>
        /// Gets or sets the max number of results to return.
        /// </summary>
        [JsonProperty("MaxCount")]
        public IntExpression MaxCount { get; set; }

        /// <summary>
        /// Gets or sets the fields to select from the Graph API.
        /// </summary>
        [JsonProperty("PropertiesToSelect")]
        public ArrayExpression<string> PropertiesToSelect { get; set; }

        /// <inheritdoc/>
        public override string DeclarativeType => GetDirectReports.GetDirectReportsDeclarativeType;

        /// <inheritdoc/>
        internal override async Task<IEnumerable<DirectoryObject>> CallGraphServiceWithResultAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            string userId = (string)parameters["UserId"];
            int maxCount = (int)parameters["MaxResults"];
            string propertiesToSelect = (string)parameters["PropertiesToSelect"];

            // Create the request first then apply the Top() after
            IUserDirectReportsCollectionWithReferencesRequest request = client.Users[userId].DirectReports.Request().Select(propertiesToSelect);

            if (maxCount > 0)
            {
                request = request.Top(maxCount);
            }

            IUserDirectReportsCollectionWithReferencesPage result = await request.GetAsync(cancellationToken).ConfigureAwait(false);

            // Again only return the top N results but discard the other pages if the manager has more than N direct reports
            return result.CurrentPage;
        }

        /// <inheritdoc />
        protected override void PopulateParameters(DialogStateManager state, Dictionary<string, object> parameters)
        {
            if (this.UserId == null)
            {
                throw new InvalidOperationException($"GetDirectReports requires UserId property.");
            }

            int maxCount = DefaultMaxCount;

            if (this.MaxCount != null)
            {
                // The TryParse will reset the value to 0 if parse fail
                maxCount = this.MaxCount.GetValue(state);
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

            string userId = this.UserId.GetValue(state);

            parameters.Add("UserId", userId);
            parameters.Add("MaxResults", maxCount);
            parameters.Add("PropertiesToSelect", propertiesToSelect);
        }

        /// <inheritdoc />
        protected override void HandleServiceException(ServiceException ex)
        {
            // Not found is a perfectly valid error in the case the person is
            // bottom of the org chart. In this case we can just return a default(DirectoryObject) == null
            // back to the caller.
            if (ex.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                base.HandleServiceException(ex);
            }
        }
    }
}
