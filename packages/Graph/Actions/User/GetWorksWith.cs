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
    /// Get people who works with me from Graph.
    /// </summary>
    [GraphCustomActionRegistration(GetWorksWith.GetWorksWithMeDeclarativeType)]
    public class GetWorksWith : BaseMsGraphCustomAction<IEnumerable<Person>>
    {
        /// <summary>
        /// Declarative type for the custom action.
        /// </summary>
        private const string GetWorksWithMeDeclarativeType = "Microsoft.Graph.User.GetWorksWith";

        /// <summary>
        /// Default max number of results to return.
        /// </summary>
        private const int DefaultMaxCount = 15;

        /// <inheritdoc/>
        public override string DeclarativeType => GetWorksWith.GetWorksWithMeDeclarativeType;

        /// <summary>
        /// Gets or sets the max number of results to return.
        /// </summary>
        [JsonProperty("MaxCount")]
        public IntExpression MaxCount { get; set; }

        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        [JsonProperty("UserId")]
        public StringExpression UserId { get; set; }

        /// <inheritdoc/>
        internal override async Task<IEnumerable<Person>> CallGraphServiceWithResultAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            string userId = (string)parameters["UserId"];
            int maxCount = (int)parameters["MaxResults"];
            IUserPeopleCollectionPage result = await client.Users[userId].People.Request().Top(maxCount).GetAsync();

            return result.CurrentPage;
        }

        /// <inheritdoc />
        protected override void PopulateParameters(DialogStateManager state, Dictionary<string, object> parameters)
        {
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
    }
}
