// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.Graph.Actions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
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
    [GraphCustomActionRegistration(GetPhoto.GetPhotorDeclarativeType)]
    public class GetPhoto : BaseMsGraphCustomAction<string>
    {
        /// <summary>
        /// Declarative type for the custom action.
        /// </summary>
        private const string GetPhotorDeclarativeType = "Microsoft.Graph.Photo.GetPhoto";

        /// <summary>
        /// Gets or sets the max number of results to return.
        /// </summary>
        [JsonProperty("UserId")]
        public StringExpression UserId { get; set; }

        /// <inheritdoc/>
        public override string DeclarativeType => GetPhoto.GetPhotorDeclarativeType;

        /// <inheritdoc/>
        internal override async Task<string> CallGraphServiceWithResultAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            string userId = (string)parameters["UserId"];
            using (Stream result = await client.Users[userId].Photo.Content.Request().GetAsync(cancellationToken).ConfigureAwait(false))
            {
                using (BinaryReader binaryReader = new BinaryReader(result))
                {
                    byte[] photoBinaryData = binaryReader.ReadBytes((int)result.Length);

                    return Convert.ToBase64String(photoBinaryData);
                }
            }
        }

        /// <inheritdoc />
        protected override void HandleServiceException(ServiceException ex)
        {
            // If there is any exception with getting photo, don't throw. Just keep
            // showing the rest of the profile.
        }

        /// <inheritdoc />
        protected override void PopulateParameters(DialogStateManager state, Dictionary<string, object> parameters)
        {
            if (this.UserId == null)
            {
                throw new InvalidOperationException($"GetPhoto requires UserId property populated.");
            }

            string userId = this.UserId.GetValue(state);

            parameters.Add("UserId", userId);
        }
    }
}
