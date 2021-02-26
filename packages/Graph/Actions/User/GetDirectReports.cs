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
        private const string GetDirectReportsDeclarativeType = "Microsoft.Graph.Calendar.GetDirectReports";

        /// <summary>
        /// Default max number of results to return.
        /// </summary>
        private const int DefaultMaxCount = 15;

        /// <summary>
        /// Gets or sets the max number of results to return.
        /// </summary>
        [JsonProperty("UserId")]
        public StringExpression UserId { get; set; }

        /// <summary>
        /// Gets or sets the max number of results to return.
        /// </summary>
        [JsonProperty("MaxCount")]
        public StringExpression MaxCount { get; set; }

        /// <inheritdoc/>
        public override string DeclarativeType => FindUsers.FindUsersDeclarativeType;

        /// <inheritdoc/>
        internal override async Task<IEnumerable<DirectoryObject>> CallGraphServiceWithResultAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            string userId = (string)parameters["UserId"];
            int maxCount = (int)parameters["MaxResults"];

            IUserDirectReportsCollectionWithReferencesPage result = await client.Users[userId].DirectReports.Request().Top(maxCount).GetAsync();

            // Again only return the top N results but discard the other pages if the manager has more than N direct reports
            return result.CurrentPage;
        }

        /// <inheritdoc />
        protected override void PopulateParameters(DialogStateManager state, Dictionary<string, object> parameters)
        {
            if (this.UserId == null)
            {
                throw new ArgumentNullException(nameof(this.UserId));
            }

            if (this.MaxCount == null || !int.TryParse(this.MaxCount.GetValue(state), out int maxCount))
            {
                // The TryParse will reset the value to 0 if parse fail
                maxCount = DefaultMaxCount;
            }

            string userId = this.UserId.GetValue(state);

            parameters.Add("UserId", userId);
            parameters.Add("MaxResults", maxCount);
        }
    }
}
