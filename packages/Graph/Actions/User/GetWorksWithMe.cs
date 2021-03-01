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
    [GraphCustomActionRegistration(GetWorksWithMe.GetWorksWithMeDeclarativeType)]
    public class GetWorksWithMe : BaseMsGraphCustomAction<IEnumerable<Person>>
    {
        /// <summary>
        /// Declarative type for the custom action.
        /// </summary>
        private const string GetWorksWithMeDeclarativeType = "Microsoft.Graph.Calendar.GetWorksWithMe";

        /// <summary>
        /// Default max number of results to return.
        /// </summary>
        private const int DefaultMaxCount = 15;

        /// <inheritdoc/>
        public override string DeclarativeType => FindUsers.FindUsersDeclarativeType;

        /// <summary>
        /// Gets or sets the max number of results to return.
        /// </summary>
        [JsonProperty("MaxCount")]
        public StringExpression MaxCount { get; set; }

        /// <inheritdoc/>
        internal override async Task<IEnumerable<Person>> CallGraphServiceWithResultAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            int maxCount = (int)parameters["MaxResults"];
            IUserPeopleCollectionPage result = await client.Me.People.Request().Top(maxCount).GetAsync();

            return result.CurrentPage;
        }

        /// <inheritdoc />
        protected override void PopulateParameters(DialogStateManager state, Dictionary<string, object> parameters)
        {
            if (this.MaxCount == null || !int.TryParse(this.MaxCount.GetValue(state), out int maxCount))
            {
                // The TryParse will reset the value to 0 if parse fail
                maxCount = DefaultMaxCount;
            }

            parameters.Add("MaxResults", maxCount);
        }
    }
}
