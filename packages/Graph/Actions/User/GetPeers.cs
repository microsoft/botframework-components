// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.Graph.Actions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
    [GraphCustomActionRegistration(GetPeers.GetPeersDeclarativeType)]
    public class GetPeers : BaseMsGraphCustomAction<IEnumerable<DirectoryObject>>
    {
        /// <summary>
        /// Declarative type for the custom action.
        /// </summary>
        private const string GetPeersDeclarativeType = "Microsoft.Graph.User.GetPeers";

        /// <summary>
        /// Default max number of results to return.
        /// </summary>
        private const int DefaultMaxCount = 15;

        /// <summary>
        /// Gets or sets the max number of results to return.
        /// </summary>
        [JsonProperty("MaxCount")]
        public IntExpression MaxCount { get; set; }

        /// <summary>
        /// Gets or sets the max number of results to return.
        /// </summary>
        [JsonProperty("UserId")]
        public StringExpression UserId { get; set; }

        /// <inheritdoc/>
        public override string DeclarativeType => GetPeers.GetPeersDeclarativeType;

        /// <inheritdoc/>
        internal override async Task<IEnumerable<DirectoryObject>> CallGraphServiceWithResultAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            // Get the user's manager
            GetManager managerAction = new GetManager();
            DirectoryObject manager = await managerAction.CallGraphServiceWithResultAsync(client, parameters, cancellationToken);

            // Now get the manager's direct report to get the user's peers
            var newParameters = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { "UserId", manager.Id },
                { "MaxResults", parameters["MaxResults"] },
            };

            GetDirectReports directReportActions = new GetDirectReports();
            IEnumerable<DirectoryObject> result = await directReportActions.CallGraphServiceWithResultAsync(client, newParameters, cancellationToken);

            return result.Where(obj => obj.Id != (string)parameters["UserId"]);
        }

        /// <inheritdoc />
        protected override void PopulateParameters(DialogStateManager state, Dictionary<string, object> parameters)
        {
            if (this.UserId == null)
            {
                throw new ArgumentNullException(nameof(this.UserId));
            }

            int maxCount = DefaultMaxCount;

            if (this.MaxCount != null)
            {
                // The TryParse will reset the value to 0 if parse fail
                maxCount = this.MaxCount.GetValue(state);
            }

            string userId = this.UserId.GetValue(state);

            parameters.Add("UserId", userId);
            parameters.Add("MaxResults", maxCount);
        }

        /// <inheritdoc />
        protected override void HandleServiceException(ServiceException ex)
        {
            // It is possible someone traverse to top of org chart to find the peers (e.g. CEO doens't have peers)
            // So in this case we will just return null to indicate nothing was found for peers.
            if (ex.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                base.HandleServiceException(ex);
            }
        }
    }
}
