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
    [GraphCustomActionRegistration(GetManager.GetManagerDeclarativeType)]
    public class GetManager : BaseMsGraphCustomAction<DirectoryObject>
    {
        /// <summary>
        /// Declarative type for the custom action.
        /// </summary>
        private const string GetManagerDeclarativeType = "Microsoft.Graph.User.GetManager";

        /// <summary>
        /// Gets or sets the max number of results to return.
        /// </summary>
        [JsonProperty("UserId")]
        public StringExpression UserId { get; set; }

        /// <inheritdoc/>
        public override string DeclarativeType => GetManager.GetManagerDeclarativeType;

        /// <inheritdoc/>
        internal override async Task<DirectoryObject> CallGraphServiceWithResultAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            string userId = (string)parameters["UserId"];
            // TODO: Make this SELECT() clause configurable for different column names
            // EXPLAINER: The reason we are limiting the field is for two reasons:
            //            1. Limit the size of the graph object payload needed to be transferred over the wire
            //            2. Reduces the exposure of end user PII (Personally Identifiable Information) in our system for privacy reasons. It's generally good practice to use what you need.
            DirectoryObject result = await client.Users[userId].Manager.Request().Select("id,displayName,mail,businessPhones,department,jobTitle,officeLocation").GetAsync(cancellationToken);

            return result;
        }

        /// <inheritdoc />
        protected override void PopulateParameters(DialogStateManager state, Dictionary<string, object> parameters)
        {
            if (this.UserId == null)
            {
                throw new ArgumentNullException(nameof(this.UserId));
            }

            string userId = this.UserId.GetValue(state);

            parameters.Add("UserId", userId);
        }

        /// <inheritdoc />
        protected override void HandleServiceException(ServiceException ex)
        {
            // Not found is a perfectly valid error in the case the person is
            // top of the org chart. In this case we can just return a default(DirectoryObject) == null
            // back to the caller.
            if (ex.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                base.HandleServiceException(ex);
            }
        }
    }
}
