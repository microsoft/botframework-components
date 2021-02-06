// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Component.MsGraph.Actions.MSGraph
{
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// This action gets user settings from MS Graph.
    /// These include the user's display name and mailboxSettings (which includes timezone).
    /// </summary>
    [MsGraphCustomActionRegistration(GetProfile.GetProfileDeclarativeType)]
    public class GetProfile : BaseMsGraphCustomAction<User>
    {
        public const string GetProfileDeclarativeType = "Microsoft.Graph.User.GetProfile";

        [JsonConstructor]
        public GetProfile([CallerFilePath] string callerPath = "", [CallerLineNumber] int callerLine = 0)
            : base(callerPath, callerLine)
        {
        }

        public override string DeclarativeType => GetProfileDeclarativeType;

        protected override async Task<User> CallGraphServiceWithResultAsync(GraphServiceClient client, DialogContext dc, CancellationToken cancellationToken)
        {
            return await client.Me.Request().GetAsync(cancellationToken);
        }
    }
}
