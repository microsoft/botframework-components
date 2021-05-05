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
    /// Get profile of other users in MSGraph.
    /// </summary>
    [GraphCustomActionRegistration(GetUserProfile.GetUserProfileDeclarativeType)]
    public class GetUserProfile : BaseMsGraphCustomAction<User>
    {
        /// <summary>
        /// Declarative type for the custom action.
        /// </summary>
        private const string GetUserProfileDeclarativeType = "Microsoft.Graph.User.GetUserProfile";

        /// <summary>
        /// Gets or sets the max number of results to return.
        /// </summary>
        [JsonProperty("UserId")]
        public StringExpression UserId { get; set; }

        /// <summary>
        /// Gets or sets the properties to select from the Graph API.
        /// </summary>
        [JsonProperty("PropertiesToSelect")]
        public ArrayExpression<string> PropertiesToSelect { get; set; }

        /// <inheritdoc/>
        public override string DeclarativeType => GetUserProfile.GetUserProfileDeclarativeType;

        /// <inheritdoc/>
        internal override async Task<User> CallGraphServiceWithResultAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            string userId = (string)parameters["UserId"];
            string propertiesToSelect = (string)parameters["PropertiesToSelect"];

            User result = await client.Users[userId].Request().Select(propertiesToSelect).GetAsync().ConfigureAwait(false);

            return result;
        }

        /// <inheritdoc />
        protected override void PopulateParameters(DialogStateManager state, Dictionary<string, object> parameters)
        {
            if (this.UserId == null)
            {
                throw new InvalidOperationException($"GetUserProfile requires UserId property.");
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
            parameters.Add("PropertiesToSelect", propertiesToSelect);
        }
    }
}
