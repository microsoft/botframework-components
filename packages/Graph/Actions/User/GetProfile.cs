// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.Graph.Actions
{
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Components.Graph;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// This action gets user settings from MS Graph.
    /// These include the user's display name and mailboxSettings (which includes timezone).
    /// </summary>
    [GraphCustomActionRegistration(GetProfile.GetProfileDeclarativeType)]
    public class GetProfile : BaseMsGraphCustomAction<User>
    {
        private const string GetProfileDeclarativeType = "Microsoft.Graph.User.GetProfile";

        /// <summary>
        /// Initializes a new instance of the <see cref="GetProfile"/> class.
        /// </summary>
        /// <param name="callerPath">Caller path.</param>
        /// <param name="callerLine">Caller line.</param>
        [JsonConstructor]
        public GetProfile([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        /// <inheritdoc/>
        public override string DeclarativeType => GetProfileDeclarativeType;

        /// <inheritdoc/>
        internal override async Task<User> CallGraphServiceWithResultAsync(IGraphServiceClient client, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken)
        {
            return await client.Me.Request().GetAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
