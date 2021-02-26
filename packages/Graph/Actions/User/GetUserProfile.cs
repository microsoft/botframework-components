﻿// Copyright (c) Microsoft Corporation. All rights reserved.
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
    public class GetUserProfile : BaseMsGraphCustomAction<Profile>
    {
        /// <summary>
        /// Declarative type for the custom action.
        /// </summary>
        private const string GetUserProfileDeclarativeType = "Microsoft.Graph.Calendar.GetUserProfile";

        /// <summary>
        /// Gets or sets the max number of results to return.
        /// </summary>
        [JsonProperty("UserId")]
        public StringExpression UserId { get; set; }

        /// <inheritdoc/>
        public override string DeclarativeType => FindUsers.FindUsersDeclarativeType;

        /// <inheritdoc/>
        internal override async Task<Profile> CallGraphServiceWithResultAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            string userId = (string)parameters["UserId"];
            Profile result = await client.Users[userId].Profile.Request().GetAsync();

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
    }
}
