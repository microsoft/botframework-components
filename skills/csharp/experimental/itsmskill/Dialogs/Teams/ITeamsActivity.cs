// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using ITSMSkill.Models.UpdateActivity;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace ITSMSkill.Dialogs.Teams
{
    /// <summary>
    /// Interface for Teams specific activity
    /// </summary>
    public interface ITeamsActivity<T>
    {
        Task<ResourceResponse> UpdateTaskModuleActivityAsync(
            ITurnContext context,
            ActivityReference activityReference,
            T updateWithValue,
            CancellationToken cancellationToken);
    }
}
