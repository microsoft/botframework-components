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

        /// <summary>
        /// Gets or sets the fields to select from the Graph API.
        /// </summary>
        [JsonProperty("FieldsToSelect")]
        public StringExpression FieldsToSelect { get; set; }

        /// <inheritdoc/>
        public override string DeclarativeType => GetManager.GetManagerDeclarativeType;

        /// <inheritdoc/>
        internal override async Task<DirectoryObject> CallGraphServiceWithResultAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            string userId = (string)parameters["UserId"];
            string fieldsToSelect = (string)parameters["FieldsToSelect"];

            DirectoryObject result = await client.Users[userId].Manager.Request().Select(fieldsToSelect).GetAsync(cancellationToken);

            return result;
        }

        /// <inheritdoc />
        protected override void PopulateParameters(DialogStateManager state, Dictionary<string, object> parameters)
        {
            if (this.UserId == null)
            {
                throw new ArgumentNullException(nameof(this.UserId));
            }

            // Select minimum of the "id" field from the object
            string fieldsToSelect = DefaultIdField;

            if (this.FieldsToSelect != null)
            {
                fieldsToSelect = this.FieldsToSelect.GetValue(state);
            }

            string userId = this.UserId.GetValue(state);

            parameters.Add("UserId", userId);
            parameters.Add("FieldsToSelect", fieldsToSelect);
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
