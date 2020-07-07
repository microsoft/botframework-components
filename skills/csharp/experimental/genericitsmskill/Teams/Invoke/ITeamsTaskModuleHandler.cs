// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;

namespace GenericITSMSkill.Teams.Invoke
{
    /// <summary>
    /// Interface for handling Fetch/Submit TaskModule
    /// </summary>
    public interface ITeamsTaskModuleHandler<T> : ITeamsFetchActivityHandler<T>, ITeamsSubmitActivityHandler<T>
    {
    }

    public interface ITeamsFetchActivityHandler<T>
    {
        Task<T> OnTeamsTaskModuleFetchAsync(ITurnContext context, CancellationToken cancellationToken);
    }

    public interface ITeamsSubmitActivityHandler<T>
    {
        Task<T> OnTeamsTaskModuleSubmitAsync(ITurnContext context, CancellationToken cancellationToken);
    }
}
